##############
# ECS Fargate Service (replacing App Runner)
##############

# ECS Cluster
resource "aws_ecs_cluster" "api" {
  name = var.repo_name
  tags = var.tags

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/${var.repo_name}"
  retention_in_days = 7
  tags              = var.tags
}

# ECS Task Definition
resource "aws_ecs_task_definition" "api" {
  family                   = var.repo_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = aws_iam_role.ecs_execution_role.arn
  task_role_arn           = aws_iam_role.apprunner_instance_role.arn
  tags                    = var.tags

  container_definitions = jsonencode([
    {
      name  = var.repo_name
      image = local.image_uri
      
      portMappings = [
        {
          containerPort = 8080
          protocol      = "tcp"
        }
      ]

      environment = [
        {
          name  = "AWS_REGION"
          value = var.region
        },
        {
          name  = "UPLOAD_BUCKET"
          value = data.aws_s3_bucket.uploads.bucket
        },
        {
          name  = "DYNAMODB_TABLE_NAME"
          value = aws_dynamodb_table.file_uploads.name
        },
        {
          name  = "ASPNETCORE_URLS"
          value = "http://+:8080"
        }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.api.name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs"
        }
      }

      essential = true
    }
  ])

  depends_on = [null_resource.build_and_push]
}

# Default VPC and subnets (for simplicity)
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
  
  filter {
    name   = "default-for-az"
    values = ["true"]
  }
}

# Security Group for Load Balancer
resource "aws_security_group" "alb" {
  name_prefix = "${var.repo_name}-alb-"
  vpc_id      = data.aws_vpc.default.id
  tags        = var.tags

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Security Group for ECS
resource "aws_security_group" "ecs" {
  name_prefix = "${var.repo_name}-ecs-"
  vpc_id      = data.aws_vpc.default.id
  tags        = var.tags

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Application Load Balancer
resource "aws_lb" "api" {
  name               = var.repo_name
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = data.aws_subnets.default.ids
  tags               = var.tags
}

# ALB Target Group
resource "aws_lb_target_group" "api" {
  name        = "file-uploader-api-port8080"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = data.aws_vpc.default.id
  target_type = "ip"
  tags        = var.tags

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200,404"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }
}

# ALB Listener
resource "aws_lb_listener" "api" {
  load_balancer_arn = aws_lb.api.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api.arn
  }
}

# ECS Service
resource "aws_ecs_service" "api" {
  name            = var.repo_name
  cluster         = aws_ecs_cluster.api.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type     = "FARGATE"
  tags            = var.tags

  network_configuration {
    subnets          = data.aws_subnets.default.ids
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = var.repo_name
    container_port   = 8080
  }

  depends_on = [aws_lb_listener.api]
}