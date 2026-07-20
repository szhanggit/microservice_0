# Project Overview

Build a production-ready enterprise microservice solution.

Technology Stack

* .NET 8
* C#
* ASP.NET Core Web API
* gRPC
* EF Core
* MySQL
* Docker
* Docker Compose
* Kubernetes
* AWS EKS
* Terraform
* xUnit
* FluentAssertions
* Moq
* Serilog
* Health Checks
* Swagger
* OpenTelemetry (optional)
* Prometheus metrics (optional)

---

# Overall Architecture

```
                REST

          UserManagementGateway
                  │
                gRPC
                  │
        UserManagementService
                  │
                gRPC
                  │
        UserRepositoryService
                  │
              EF Core
                  │
               MySQL
```

---

# Root Folder

```
microservice_0
│
├── components
│
│     ├── UserManagementGateway
│     ├── UserManagementService
│     └── UserRepositoryService
│
├── libs
│
│     ├── Shared.Contracts
│     ├── Shared.Protos
│     ├── Shared.Common
│     └── Shared.Logging
│
├── kubernetes
│
│     ├── mysql
│     ├── gateway
│     ├── management
│     ├── dataaccess
│     ├── ingress
│     └── namespace
│
├── terraform
│
│     ├── modules
│     ├── environments
│     └── scripts
│
├── docker-compose.yml
│
└── README.md
```

---

# Each Microservice Structure

Example

```
UserRepositoryService

src

resources

    mysql

        create_tables.sql

tests

    unit

    integration

Dockerfile

README.md
```

All three services should follow exactly the same structure.

---

# Shared Libraries

## Shared.Contracts

DTOs

Responses

Requests

Enums

Constants

---

## Shared.Protos

grpc proto files

Generated code

---

## Shared.Common

Exceptions

Result Pattern

Utilities

Middleware

Extensions

Validators

---

## Shared.Logging

Logging Extensions

Correlation ID

Tracing

---

# Database

MySQL

```
UserInfo

------------

UserId

FirstName

LastName

Email
```

---

# Business Functions

Gateway exposes REST

```
POST /users

PUT /users/{id}

DELETE /users/{id}

GET /users/{id}

GET /users?name=abc
```

Gateway calls ManagementService

ManagementService validates business rules

ManagementService calls DataAccessService

DataAccessService accesses MySQL

---

# Dependency Injection

Every layer must use DI.

No static helper classes except extensions.

---

# Unit Tests

Every business class has tests.

Use

* xUnit

* FluentAssertions

* Moq

Coverage

* Success

* Invalid input

* Exceptions

* Not Found

* Duplicate Email

---

# Docker

Each service

```
Dockerfile
```

Root

```
docker-compose.yml
```

Compose should start

Gateway

Management

DataAccess

MySQL

---

# Kubernetes

Every service has

Deployment

Service

ConfigMap

Secret

HPA

PDB

Resource Limits

Readiness Probe

Liveness Probe

Startup Probe

Namespace

Ingress

---

# Terraform

Create

VPC

EKS

Node Group

IAM

ECR

CloudWatch

Security Groups

Outputs

Variables

Remote Backend

---

# Logging

Serilog

Structured Logging

Correlation ID

---

# Health Checks

```
/health

/ready

/live
```

---

# API Documentation

Swagger

OpenAPI

---

# Readme

Include

Architecture Diagram

Folder Structure

Docker

Docker Compose

Local Run

Kubernetes

Terraform

Deploy to EKS

Testing

Screenshots placeholder

---

# Coding Standards

Use the coding standards defined previously.

Always generate

* nullable enabled

* C#

* file scoped namespace

* primary constructor when appropriate

* async/await

* cancellation token

* ProblemDetails

* Dependency Injection

* Clean Architecture

* SOLID

* production ready

---

# Claude Code TODO List

I would **not** ask Claude to generate the entire project in one prompt. Instead, have it complete one milestone at a time. A good breakdown is:

## Phase 1 – Solution Skeleton ✅ DONE

* Create the root folder structure.
* Create the solution (`.sln`).
* Create the three ASP.NET Core projects and shared libraries.
* Add project references and NuGet packages.
* Enable nullable reference types and common analyzers.
* Configure shared `Directory.Build.props` and `Directory.Packages.props` (optional).

**Deliverable:** A solution that builds successfully.

---

## Phase 2 – Shared Libraries

* Implement `Shared.Contracts` (DTOs, requests, responses, enums).
* Define gRPC `.proto` files in `Shared.Protos`.
* Add common result types, exceptions, and utilities.
* Implement shared logging extensions.

**Deliverable:** Shared code compiles and is referenced by all services.

---

## Phase 3 – UserRepositoryService

* Create the EF Core `DbContext`.
* Define the `UserInfo` entity.
* Implement repositories (or direct EF Core if you choose not to use the repository pattern).
* Add MySQL initialization script.
* Expose gRPC endpoints for CRUD and search.
* Write unit tests for business logic.
* Add integration tests for database access.

**Deliverable:** Service runs locally and passes tests.

---

## Phase 4 – UserManagementService

* Implement business validation (duplicate email, required fields, etc.).
* Consume `UserRepositoryService` via gRPC.
* Expose its own gRPC API.
* Write comprehensive unit tests.

**Deliverable:** Business layer works independently.

---

## Phase 5 – UserManagementGateway

* Build REST endpoints.
* Generate Swagger documentation.
* Call `UserManagementService` using gRPC.
* Add request validation and global exception handling.
* Write unit tests for controllers and services.

**Deliverable:** End-to-end REST API works locally.

---

## Phase 6 – Docker

* Create optimized multi-stage Dockerfiles for each service.
* Write a root `docker-compose.yml`.
* Ensure the full stack (three services + MySQL) starts with a single command.

**Deliverable:** `docker compose up` brings up the entire application.

---

## Phase 7 – Kubernetes

* Create manifests for Namespace, ConfigMaps, Secrets, Deployments, Services, Ingress, HPAs, and PodDisruptionBudgets.
* Add readiness, liveness, and startup probes.
* Verify deployment on a local Kubernetes cluster (optional) before EKS.

**Deliverable:** Kubernetes manifests are production-ready.

---

## Phase 8 – Terraform

* Study every single file in \\wsl.localhost\Ubuntu-22.04\home\steven\terraform\11.eks.basic and exactly follow the design pattern. 
* Build reusable Terraform modules for networking, EKS, IAM, ECR, and supporting infrastructure.
* Configure variables, outputs, and remote state.
* Push container images to ECR and deploy to EKS.

**Deliverable:** Infrastructure can be provisioned and the application deployed with Terraform.

---

## Phase 9 – Documentation and Polish

* Write a comprehensive `README.md`.
* Add architecture and sequence diagrams.
* Document local development, Docker Compose, Kubernetes, Terraform, and EKS deployment steps.
* Include troubleshooting and future enhancement ideas.

**Deliverable:** A polished repository suitable for interviews or as a portfolio project.