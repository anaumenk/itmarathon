terraform {
  backend "s3" {
    bucket       = "terraform-tfstate-qwerty"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
