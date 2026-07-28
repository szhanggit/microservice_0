# Security group allowing inbound MySQL access from inside the VPC only.
# (The reference tutorial this module is based on opened 3306 to 0.0.0.0/0;
# scoped down to the VPC CIDR here since nothing outside the cluster's VPC
# needs to reach this database.)
resource "aws_security_group" "rds" {
  name        = var.db_security_group_name
  description = "Allow MySQL access from within the microservice0 VPC"
  vpc_id      = var.vpc_id

  ingress {
    description = "MySQL from within the VPC"
    from_port   = var.db_port
    to_port     = var.db_port
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr_block]
  }

  tags = {
    Name = var.db_security_group_name
  }
}

resource "aws_db_subnet_group" "rds" {
  name        = var.db_subnet_group_name
  description = "microservice0 RDS DB subnet group"
  subnet_ids  = var.private_subnet_ids

  tags = {
    Name = var.db_subnet_group_name
  }
}

# Role RDS assumes to publish OS-level metrics (CPU, memory, disk I/O per
# process) to CloudWatch Logs when Enhanced Monitoring is enabled - separate
# from any application-facing IAM, this is purely RDS-to-CloudWatch.
data "aws_iam_policy_document" "rds_enhanced_monitoring_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["monitoring.rds.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "rds_enhanced_monitoring" {
  name               = "${var.db_instance_identifier}-enhanced-monitoring-role"
  assume_role_policy = data.aws_iam_policy_document.rds_enhanced_monitoring_assume_role.json
}

resource "aws_iam_role_policy_attachment" "rds_enhanced_monitoring" {
  role       = aws_iam_role.rds_enhanced_monitoring.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonRDSEnhancedMonitoringRole"
}

resource "aws_db_instance" "main" {
  identifier     = var.db_instance_identifier
  engine         = "mysql"
  engine_version = var.db_engine_version

  # Creates the "userdb" application database as part of instance
  # provisioning - dataaccess's create_tables.sql then runs against it on
  # first app startup, same as it does against the docker-compose mysql
  # container locally.
  db_name = var.db_name

  instance_class    = var.db_instance_class
  allocated_storage = var.db_allocated_storage
  storage_type      = var.db_storage_type

  username = var.db_master_username
  password = var.db_master_password
  port     = var.db_port

  db_subnet_group_name   = aws_db_subnet_group.rds.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = var.db_publicly_accessible
  multi_az               = false

  # Disabled so a destroy never has to wait behind a backup in progress - this
  # is a learning/portfolio environment, not something needing PITR.
  backup_retention_period = 0

  skip_final_snapshot = true

  monitoring_interval = var.monitoring_interval
  monitoring_role_arn = var.monitoring_interval > 0 ? aws_iam_role.rds_enhanced_monitoring.arn : null

  # Performance Insights requires at least db.t3.medium for MySQL on this
  # engine version (confirmed via `aws rds describe-orderable-db-instance-options`
  # - db.t3.micro/small both report SupportsPerformanceInsights=false).
  performance_insights_enabled          = var.performance_insights_enabled
  performance_insights_retention_period = var.performance_insights_enabled ? var.performance_insights_retention_period : null
}
