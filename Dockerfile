FROM microsoft/dotnet:2.2.103-sdk AS build-env
WORKDIR /app

COPY ./. ./

RUN dotnet restore Bizanc.io.Matching.sln 
RUN mkdir out
RUN dotnet publish Bizanc.io.Matching.App/Bizanc.io.Matching.App.csproj -c Release -o ./out

# build runtime image
FROM microsoft/dotnet:2.2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env app/Bizanc.io.Matching.App/out ./
ENTRYPOINT ["dotnet", "Bizanc.io.Matching.App.dll"]