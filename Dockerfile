FROM mcr.microsoft.com/dotnet/core/sdk:2.2.203 AS build-env
WORKDIR /app

COPY ./. ./

RUN dotnet restore Bizanc.io.Matching.sln 
RUN mkdir out
RUN dotnet publish Bizanc.io.Matching.App/Bizanc.io.Matching.App.csproj -c Release -o ./out

# build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.4
WORKDIR /app
COPY --from=build-env app/Bizanc.io.Matching.App/out ./
ENTRYPOINT ["dotnet", "Bizanc.io.Matching.App.dll"]