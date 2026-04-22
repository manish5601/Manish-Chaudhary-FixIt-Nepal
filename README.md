# FixIt Nepal 🛠️

FixIt Nepal is an on-demand service marketplace platform designed to connect skilled service providers with customers in Nepal. The platform simplifies the process of finding, booking, and paying for household and professional services.

##  Key Features

### For Customers
- **Service Discovery**: Browse services by categories or find nearby providers using geographical maps.
- **Booking System**: Select preferred time slots and book services instantly.
- **eSewa Integration**: Securely pay a token amount (Rs. 10) to confirm bookings.
- **Real-time Chat**: Communicate directly with service providers via integrated live chat.
- **Reviews & Ratings**: Rate and review providers after the service is complete.

### For Service Providers
- **Professional Profile**: Showcase your skills, experience, and specific service areas.
- **Verification System**: Upload citizenship or professional documents to get approved by admins.
- **Availability Management**: Set your working days and hours to automate booking slots.
- **Booking Dashboard**: Conveniently accept, reject, or manage ongoing customer bookings.

### For Admins
- **User Management**: Oversee all the registered customers and service providers.
- **Verification Portal**: Review, approve, or reject provider applications and their submitted documents.
- **Moderation**: Monitor the platform activity and manage flagged reviews or user disputes.

##  Technology Stack
- **Framework**: Built with ASP.NET Core 9.0 MVC
- **Database**: Managed using Entity Framework Core and SQL Server
- **Real-time**: Real-time chat powered by SignalR
- **Security**: Authentication and security handled by ASP.NET Core Identity & JWT
- **Integrations**: External API integrations include the eSewa SDK for payments and Google Maps for mapping features

##  Getting Started: How to Run the Project

### Prerequisites
Before running the project, make sure you have the following installed on your computer:
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or LocalDB)

### Step-by-Step Installation

1. **Download the Project**
   First, download or clone the project repository from GitHub to your local machine, and then open the project folder.
   ```bash
   git clone https://github.com/manish5601/Manish-Chaudhary-Fixit-Nepal.git
   cd FixItNepal
   ```

2. **Configure Your Settings**
   Open the `appsettings.json` file in the project folder and update it with your own local credentials:
   - **ConnectionStrings**: Enter your SQL Server database connection string.
   - **EmailSettings**: Add your Email SMTP credentials to allow the application to send emails.
   - **ESewaSettings**: Add your eSewa Merchant credentials to process payments.
   - **GoogleMaps**: Enter your Google Maps API Key to enable the mapping features.

3. **Initialize the Database**
   Open your command prompt or terminal inside the project folder. Run the following command to automatically create the required database tables:
   ```bash
   dotnet ef database update
   ```
   *(Alternatively, if you are using Visual Studio, you can run `Update-Database` in the Package Manager Console).*

4. **Run the Application**
   Finally, start the application by running the following command in your terminal:
   ```bash
   dotnet run
   ```
   *(If you are using Visual Studio, simply open `FixItNepal.csproj` and press the "Start" or "Run" button at the top).*

##  Security Reminder
The project uses JSON Web Tokens (JWT) for API authentication and Identity Cookies for the web portal. Always ensure that the secret keys (`JwtSettings:Key`) in your `appsettings.json` file are kept absolutely safe and private in production environments.
