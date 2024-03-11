Amazon Price Tracker

This project is a product price tracker for Amazon, developed in C# and utilizing Playwright. It checks if the current price of a list of products is below a specified value and, if so, sends an alert email to a list of recipients. The process is repeated every 30 minutes.

Requirements

    .NET 6.0 or higher
    Playwright for .NET

Installation

    Clone the repository or download the project to your local machine.
    In the project directory, run dotnet restore to install dependencies.
    Create a file named email_credentials.json in the project's root directory with the email credentials that will be used to send notifications (example content below):
    Currently, only Hotmail emails are supported

{

  "Email": "your_email@example.com",
  
  "Password": "your_password"
  
}

    Create a file named email_recipients.txt in the project's root directory containing a list of emails that will receive notifications. Place one email per line, as in the example below:
	
recipient1@example.com

recipient2@example.com

    Create a file named products.txt in the project's root directory containing a list of product URLs you want to track and their respective target prices. Place one product per line, separating the URL and target price by a comma, as in the example below:

https://www.amazon.com.br/Product1/dp/XXXXX, 100

https://www.amazon.com.br/Product2/dp/XXXXX, 150

    Compile and run the project with dotnet build and dotnet run, respectively.

Installing Playwright Browser

    Build the solution.
    Open PowerShell 6.0 or higher.
    Navigate to the project's build folder. Replace <PATH_TO_BUILD_FOLDER> with the correct path to the build folder on your computer.
    cd "<PATH_TO_BUILD_FOLDER>"
    Example:
    cd "C:\AmazonPriceTracker\AmazonPriceTracker\bin\Debug\net6.0"
    Run the following command to install the Playwright browser:
    pwsh playwright.ps1 install

After following these steps, the Playwright browser will be installed and ready to use in the project.

For the compiled project

Download Compiled Project

    Requires PowerShell 6.0. You can download it here: https://github.com/PowerShell/PowerShell/releases/download/v7.4.1/PowerShell-7.4.1-win-x64.msi
    Unzip the Compiled Project.zip file.
    Edit the email_credentials.json file with your Hotmail email credentials (username and password).
    Edit the email_recipients.txt file with the list of emails you want to send to.
    Edit the products.txt file with the products you want to check and the alert price.
    Run the Install Playwright.bat file.
    Wait for the installation to finish.
    Run the AmazonPriceTracker.exe file.

Features

    Track prices of Amazon products.
    Send alert emails when the product price is below the desired value.
    Read the list of products and target prices from an external file.
    Read the list of email recipients from an external file.
    Use email credentials from an external file for added security.

Limitations

    The current implementation is specific to Amazon Brazil.

Contributing

Feel free to contribute improvements or bug fixes. Fork the project, create a branch, make changes, and submit a pull request.

License

This project is licensed under the MIT License. See the LICENSE.md file for more details.