Sistema Integrador de Inventario Seguro (Project.API)
  Descripción del Proyecto
Este proyecto consiste en el desarrollo de una API RESTful segura y escalable para la gestión de inventarios. El sistema implementa una Arquitectura Limpia (Clean Architecture) utilizando Minimal APIs en .NET 8, priorizando el rendimiento, la seguridad y la automatización del despliegue.

El objetivo principal es demostrar la integración de tecnologías modernas de desarrollo Backend, contenedorización y prácticas de DevSecOps.

   Arquitectura del Sistema
El proyecto sigue un diseño modular desacoplado, separando la lógica de negocio, el acceso a datos y la exposición de endpoints.

Diagrama de Componentes
Fragmento de código

graph TD
    Client[Cliente / Frontend / Postman] -->|HTTPS| Proxy[Remote.it / Internet]
    Proxy -->|Port 8081| API[API .NET 8 Container]
    
    subgraph Docker Network [Red Privada Docker]
        API -->|Port 8080 Internal| API_Process[.NET Process]
        API -->|TDS Port 1433| SQL[SQL Server 2022 Container]
        API -->|Logs/Metrics| Kuma[Uptime Kuma Monitor]
    end

    subgraph CI/CD Pipeline
        Git[GitHub Repository] -->|Push Event| Action[GitHub Actions]
        Action -->|Self-Hosted Runner| Server[Ubuntu Server]
        Server -->|Docker Build & Run| API
    end
Patrones y Estructuras Utilizadas
Clean Architecture: Separación en capas (Models, Services, Endpoints/Program).

Minimal API: Reducción de "boilerplate" para microservicios ligeros.

Repository Pattern (implícito): Acceso a datos abstraído mediante Dapper y DbConnectionFactory.

Dependency Injection (DI): Inyección de servicios (MovimientoService, ProductoService) en el contenedor IoC.

🛠️ Stack Tecnológico
Backend & Core
Lenguaje: C# 12.

Framework: .NET 8 (ASP.NET Core Web API).

ORM: Dapper (Micro-ORM) para consultas SQL de alto rendimiento y control granular.

Autenticación: JWT (JSON Web Tokens) con esquema Bearer.

Documentación: Swagger / OpenAPI.

Base de Datos
Motor: Microsoft SQL Server 2022 (Linux Container).

Diseño: Modelo Relacional Normalizado.

Seguridad: Usuarios con privilegios mínimos.

Infraestructura & DevOps
Contenedores: Docker & Docker Compose.

SO Servidor: Ubuntu Server 24.04 LTS.

CI/CD: GitHub Actions con Self-Hosted Runner (Despliegue automático on-premise).

Acceso Remoto: Remote.it (Túnel seguro sin exposición de puertos WAN).

Monitoreo: Uptime Kuma (Health checks en tiempo real).

   Seguridad Implementada
La seguridad es el pilar central de este proyecto ("Security by Design"):

Protección de Red:

La Base de Datos NO expone puertos a Internet. Vive en una red interna de Docker y solo la API puede comunicarse con ella.

La API expone únicamente el puerto 8081 hacia el exterior.

Gestión de Secretos:

Uso de Variables de Entorno (-e) para inyectar Connection Strings y Claves JWT en tiempo de ejecución. Nada hardcodeado en el código.

Autenticación y Autorización:

Implementación de [Authorize] en endpoints críticos.

Validación de Claims y Roles (Admin vs User).

Validación de Datos:

Uso de DTOs (Data Transfer Objects) para no exponer entidades de dominio.

Validación de entradas para prevenir SQL Injection (uso de parámetros en Dapper).

   Estructura del Repositorio
Plaintext

Project-API/
├── .github/workflows/            # Pipelines de CI/CD
│   ├── build.yml                 # Compilación .NET
│   ├── docker-build.yml          # Construcción de imágenes
│   ├── docker-compose-selfhosted.yml # Despliegue en servidor
│   └── docker-publish.yml        # Publicación a Docker Hub
├── Docker/
│   └── docker-compose.yml        # Orquestación (API + BD)
├── WebAPI/                       # Código Fuente Principal (Project.API)
│   ├── Properties/               # Configuración de lanzamiento
│   ├── Models/                   # Entidades (inventario.cs, usuarios.cs)
│   ├── Services/                 # Lógica de Negocio (MovimientoService.cs)
│   ├── appsettings.json          # Configuración base
│   ├── Minimalapi.JWT.csproj     # Definición del proyecto .NET
│   └── Program.cs                # Endpoints y Configuración DI
├── Dockerfile                    # Instrucciones de construcción de imagen
└── README.md                     # Documentación Técnica
🚀 Despliegue (Pipeline CI/CD)
El proyecto cuenta con un sistema de Despliegue Continuo automatizado:

Trigger: Al hacer un git push a la rama main.

Build: El Runner en el servidor Ubuntu detecta el cambio.

Dockerization:

Se construye una nueva imagen Docker optimizada usando el Dockerfile de la raíz.

Se detiene el contenedor antiguo.

Se elimina el contenedor anterior para liberar recursos.

Deploy: Se levanta el nuevo contenedor mapeando el puerto 8081 con la configuración de entorno actualizada.

Comandos Manuales (Referencia)
Si se requiere levantar el entorno manualmente en el servidor:

Bash

# Construir imagen
docker build -t project-api:latest .

# Correr contenedor conectado a la red y BD (Mapeando 8081 externo a 8080 interno)
docker run -d \
  -p 8081:8080 \
  --name api-container \
  --network mi-red-auth \
  -e "ConnectionStrings:DefaultConnection=Server=sqlserver;Database=AuthDb;..." \
  project-api:latest
📡 Endpoints Principales
Autenticación
POST /api/auth/login: Recibe credenciales, retorna JWT.

Productos (CRUD)
GET /api/productos: Listado público.

POST /api/productos: Crear nuevo (Requiere Rol Admin).

PUT /api/productos/{id}: Actualizar stock/precio.

Inventario (Lógica de Negocio)
POST /api/inventario/movimiento: Registra entradas/salidas y actualiza Kardex (Transaccional).

   Equipo de Desarrollo
Ingeniería en Sistemas y Automatización - Curso de Titulación 2025

Daniel Gerardo Morales Vazquez

José Jaime Zurita Hernández

Profesor: Rogelio Arriaga González
