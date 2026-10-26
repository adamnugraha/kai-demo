terraform {

  required_version = ">= 1.10, < 2.0"
  required_providers {
    google = {
      source = "hashicorp/google", version = "~> 7.0"
    }

  }


}

provider "google" {
  project = var.project_id
  region  = var.region

}

variable "project_id" {
  type = string
}

variable "region" {
  default = "asia-southeast2"
}

variable "image" {
  type = string
}

variable "runtime_service_account" {
  type = string
}

variable "network" {
  type = string
}

variable "subnetwork" {
  type = string
}

variable "db_secret_id" {
  default = "kai-lab-db"
}

variable "token_secret_id" {
  default = "kai-lab-token"
}

variable "notifier_url" {
  type = string
}

variable "legacy_url" {
  default = ""
}

variable "migration_stage" {
  default = "modern"
}

locals {

  plain_env = {
    DB_PROVIDER = "sqlserver", NOTIFIER_URL = var.notifier_url, AUTO_INIT = "false", RUN_WORKER = "false", APP_VERSION = "modern-v1", LEGACY_URL = var.legacy_url, MIGRATION_STAGE = var.migration_stage
  }

  secret_env = {
    DB_CONNECTION = var.db_secret_id, LAB_TOKEN = var.token_secret_id
  }


}

resource "google_cloud_run_v2_service" "app" {

  name                = "kai-railbooking"
  location            = var.region
  deletion_protection = false
  ingress             = "INGRESS_TRAFFIC_ALL"
  template {

    service_account                  = var.runtime_service_account
    max_instance_request_concurrency = 20
    scaling {
      min_instance_count = 1
      max_instance_count = 3

    }

    vpc_access {

      egress = "PRIVATE_RANGES_ONLY"
      network_interfaces {
        network    = var.network
        subnetwork = var.subnetwork

      }


    }

    containers {

      image = var.image
      ports {
        container_port = 8080
      }

      resources {
        limits = {
          cpu = "1", memory = "512Mi"
        }

      }

      dynamic "env" {
        for_each = local.plain_env
        content {
          name  = env.key
          value = env.value

        }


      }

      dynamic "env" {
        for_each = local.secret_env
        content {
          name = env.key
          value_source {
            secret_key_ref {
              secret  = env.value
              version = "latest"

            }

          }


        }


      }

      startup_probe {
        http_get {
          path = "/health/ready"
        }

        initial_delay_seconds = 5
        period_seconds        = 10
        failure_threshold     = 12

      }

      liveness_probe {
        http_get {
          path = "/health/live"
        }

      }


    }


  }

  traffic {
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
    percent = 100

  }

  lifecycle {
    ignore_changes = [traffic]
  }


}

resource "google_cloud_run_v2_job" "init" {

  name                = "kai-railbooking-init"
  location            = var.region
  deletion_protection = false
  template {
    template {

      service_account = var.runtime_service_account
      max_retries     = 0
      timeout         = "300s"
      vpc_access {
        egress = "PRIVATE_RANGES_ONLY"
        network_interfaces {
          network    = var.network
          subnetwork = var.subnetwork

        }


      }

      containers {
        image = var.image
        args  = ["--init-db"]
        resources {
          limits = {
            cpu = "1", memory = "512Mi"
          }

        }

        dynamic "env" {
          for_each = local.plain_env
          content {
            name  = env.key
            value = env.value

          }


        }

        dynamic "env" {
          for_each = local.secret_env
          content {
            name = env.key
            value_source {
              secret_key_ref {
                secret  = env.value
                version = "latest"

              }

            }


          }


        }


      }


    }

  }


}

resource "google_cloud_run_v2_job" "dispatch" {

  name                = "kai-railbooking-dispatch"
  location            = var.region
  deletion_protection = false
  template {
    template {

      service_account = var.runtime_service_account
      max_retries     = 0
      timeout         = "300s"
      vpc_access {
        egress = "PRIVATE_RANGES_ONLY"
        network_interfaces {
          network    = var.network
          subnetwork = var.subnetwork

        }


      }

      containers {
        image = var.image
        args  = ["--dispatch-once"]
        resources {
          limits = {
            cpu = "1", memory = "512Mi"
          }

        }

        dynamic "env" {
          for_each = local.plain_env
          content {
            name  = env.key
            value = env.value

          }


        }

        dynamic "env" {
          for_each = local.secret_env
          content {
            name = env.key
            value_source {
              secret_key_ref {
                secret  = env.value
                version = "latest"

              }

            }


          }


        }


      }


    }

  }


}

output "service_url" {
  value = google_cloud_run_v2_service.app.uri
}

# No allUsers invoker binding. The lab stays IAM-authenticated.
