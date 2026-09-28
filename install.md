dotnet tool install --global dotnet-ef

# Scripts SQL base (geo + catálogo). En BD ya existente con Usuario BIGINT
# ejecutar también: database/init/012_prepare_identity.sql
bash database/install.sh

# Aplicar ASP.NET Identity (tablas HidrtbUsuario / HidrtbRol / …)
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
# Requiere ConnectionStrings__DefaultConnection (p. ej. desde .env)
dotnet ef database update \
    --project src/Hidrix.Infrastructure \
    --startup-project src/Hidrix.Api

# Opcional: definir Seed__AdminEmail / Seed__AdminPassword en .env
# para crear el admin al iniciar la API.