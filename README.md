# FixIt Nepal 🛠️

FixIt Nepal is an on-demand service marketplace platform designed to connect skilled service providers with customers in Nepal. The platform simplifies the process of finding, booking, and paying for household and professional services.

## 🚀 Features

### For Customers
- **Service Discovery**: Browse services by categories or find nearby providers using geographical maps.
- **Booking System**: Select preferred time slots and book services instantly.
- **eSewa Integration**: Securely pay a token amount (Rs. 10) to confirm bookings.
- **Real-time Chat**: Communicate directly with service providers via integrated SignalR chat.
- **Reviews & Ratings**: Rate and review providers after service completion.

### For Service Providers
- **Professional Profile**: Showcase skills, experience, and service areas.
- **Verification System**: Upload citizenship or professional documents for admin approval.
- **Availability Management**: Set working days and hours for automated booking slots.
- **Booking Dashboard**: Accept, reject, or manage ongoing bookings.

### For Admins
- **User Management**: Oversee all customers and providers.
- **Verification Portal**: Review and approve provider applications and documents.
- **Moderation**: Monitor and manage flagged reviews or disputes.

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 9.0 MVC
- **Data Access**: Entity Framework Core (SQL Server)
- **Real-time**: SignalR (Chat and Notifications)
- **Security**: ASP.NET Core Identity & JWT (for API)
- **Payment**: eSewa SDK Integration
- **Maps**: Google Maps API & Leaflet.js
- **Styling**: Vanilla CSS, Bootstrap & Modern UI Patterns

## 📂 Project Structure

- `Controllers/Api`: REST API endpoints for mobile/frontend integration.
- `Hubs`: SignalR hubs for real-time messaging.
- `Services`: Business logic for Email (SMTP) and eSewa payments.
- `Models`: Database entities (EF Core).
- `ViewModels`: Data transfer objects for views and APIs.
- `wwwroot`: Static assets (CSS, JS, Images).

## ⚙️ Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or LocalDB)

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/manish5601/Manish-Chaudhary-Fixit-Nepal.git
   cd FixItNepal
   ```

2. **Configure Settings**:
   Update `appsettings.json` with your credentials:
   - **ConnectionStrings**: Set your SQL Server connection string.
   - **EmailSettings**: SMTP credentials for notifications.
   - **ESewaSettings**: Merchant credentials for payments.
   - **GoogleMaps**: API Key for map rendering.

3. **Initialize Database**:
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**:
   ```bash
   dotnet run
   ```

## 🔐 Security Note
The project uses JWT for API authentication and Identity Cookies for the web portal. Ensure the `JwtSettings:Key` in `appsettings.json` is kept secure in production environments.
