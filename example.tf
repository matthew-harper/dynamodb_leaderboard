provider "aws" {
  profile    = "adminuser"
  region     = "us-east-2"
}

resource "aws_dynamodb_table" "basic-dynamodb-table" {
  name           = "HighScores"
  billing_mode   = "PROVISIONED"
  read_capacity  = 2
  write_capacity = 2
  hash_key       = "Username"
  range_key      = "Game"

  attribute {
    name = "Username"
    type = "S"
  }

  attribute {
    name = "Game"
    type = "S"
  }

  attribute {
    name = "TopScore"
    type = "N"
  }
 
  attribute {
    name = "Timestamp"
    type = "S"
  }

  ttl {
    attribute_name = "TimeToExist"
    enabled        = false
  }

  global_secondary_index {
    name               = "GameIndex"
    hash_key           = "Game"
    range_key          = "TopScore"
    write_capacity     = 1
    read_capacity      = 1
    projection_type    = "INCLUDE"
    non_key_attributes = ["Username"]
  }

   local_secondary_index {
    name               = "TimestampIndex"
    range_key          = "Timestamp"
    projection_type    = "ALL"
  }

  tags = {
    Name        = "dynamodb-table-1"
    Environment = "production"
  }
}