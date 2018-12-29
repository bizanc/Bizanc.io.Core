FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Bizanc.io.Matching.sln ./
COPY Bizanc.io.Matching.Core/Bizanc.io.Matching.Core.csproj ./Bizanc.io.Matching.Core/Bizanc.io.Matching.Core.csproj
COPY Bizanc.io.Matching.Test/Bizanc.io.Matching.Test.csproj ./Bizanc.io.Matching.Test/Bizanc.io.Matching.Test.csproj
RUN ls -la ./*
RUN dotnet restore

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# build runtime image
FROM microsoft/dotnet:runtime
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "Bizanc.io.Matching.Core.dll"]