# Sử dụng base image chính thức của Microsoft cho ASP.NET Core SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Sao chép file .csproj và restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ source code còn lại
COPY . ./
# Build và publish ứng dụng với cấu hình Release
# Thay thế "YourProjectName.csproj" bằng tên file .csproj thực tế của bạn
RUN dotnet publish "ZhaoYi_Test2.csproj" -c Release -o out

# Xây dựng runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Cấu hình cổng mà ứng dụng sẽ lắng nghe (thường là 8080 cho Cloud Run)
# Cloud Run sẽ tự động lắng nghe cổng được định nghĩa bởi biến môi trường PORT
# Nên bạn có thể không cần EXPOSE nếu dùng biến PORT
# EXPOSE 8080

# Entry point để chạy ứng dụng
# Thay thế "YourProjectName.dll" bằng tên file .dll thực tế của bạn
ENTRYPOINT ["dotnet", "ZhaoYi_Test2.dll"]