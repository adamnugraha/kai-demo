# Optional foundation. Creates billable SQL Server resources.
# Keep this state separate from the application. Review a plan and cost estimate before apply.
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

variable "sql_admin_password" {
  type      = string
  sensitive = true

}

variable "database_ha" {
  default = false
}

resource "google_project_service" "apis" {

  for_each           = toset(["run.googleapis.com", "compute.googleapis.com", "sqladmin.googleapis.com", "servicenetworking.googleapis.com", "artifactregistry.googleapis.com", "secretmanager.googleapis.com"])
  service            = each.value
  disable_on_destroy = false

}

resource "google_compute_network" "vpc" {

  name                    = "kai-lab"
  auto_create_subnetworks = false
  depends_on              = [google_project_service.apis]

}

resource "google_compute_subnetwork" "run" {

  name                     = "kai-lab-run"
  network                  = google_compute_network.vpc.id
  ip_cidr_range            = "10.90.0.0/24"
  region                   = var.region
  private_ip_google_access = true

}

resource "google_compute_global_address" "sql" {

  name          = "kai-lab-sql-range"
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 16
  network       = google_compute_network.vpc.id

}

resource "google_service_networking_connection" "sql" {

  network                 = google_compute_network.vpc.id
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.sql.name]

}

resource "google_sql_database_instance" "sql" {

  name                = "kai-lab-sql"
  database_version    = "SQLSERVER_2022_STANDARD"
  region              = var.region
  root_password       = var.sql_admin_password
  deletion_protection = true
  settings {

    tier              = "db-custom-2-7680"
    availability_type = var.database_ha ? "REGIONAL" : "ZONAL"
    disk_size         = 20
    disk_autoresize   = true
    ip_configuration {
      ipv4_enabled    = false
      private_network = google_compute_network.vpc.id

    }

    backup_configuration {
      enabled = true
    }


  }

  depends_on = [google_service_networking_connection.sql]

}

resource "google_sql_database" "app" {
  name     = "railbooking"
  instance = google_sql_database_instance.sql.name

}

resource "google_artifact_registry_repository" "images" {

  location      = var.region
  repository_id = "kai-workshop"
  format        = "DOCKER"
  depends_on    = [google_project_service.apis]

}

resource "google_service_account" "runtime" {
  account_id   = "kai-lab-runtime"
  display_name = "KAI workshop application runtime"

}

resource "google_secret_manager_secret" "app" {

  for_each  = toset(["kai-lab-db", "kai-lab-token"])
  secret_id = each.value
  replication {
    auto {

    }

  }

  depends_on = [google_project_service.apis]

}

resource "google_secret_manager_secret_iam_member" "read" {

  for_each  = google_secret_manager_secret.app
  secret_id = each.value.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.runtime.email}"

}

output "sql_private_ip" {
  value = google_sql_database_instance.sql.private_ip_address
}

output "runtime_service_account" {
  value = google_service_account.runtime.email
}

output "network" {
  value = google_compute_network.vpc.name
}

output "subnetwork" {
  value = google_compute_subnetwork.run.name
}

