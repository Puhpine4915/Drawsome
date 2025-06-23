using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Drawsome.Tests;

[TestFixture]
public class PasswordValidationTests
{
    private IWebDriver? _driver;
    private const string RegisterUrl = "http://localhost:5056/Home/Register";

    [SetUp]
    public void Setup()
    {
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--headless");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        var userDataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        options.AddArgument($"--user-data-dir={userDataDir}");

        _driver = new ChromeDriver(options);
        _driver.Navigate().GoToUrl(RegisterUrl);
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    [Test]
    public void ValidatePassword_TooShortPassword_ShowsError()
    {
        // Arrange
        var passwordInput = _driver!.FindElement(By.Id("password"));
        var errorDiv = _driver.FindElement(By.Id("passwordError"));

        // Act
        passwordInput.SendKeys("short");
        passwordInput.SendKeys(Keys.Tab); // Trigger onInput event

        // Assert
        Assert.That(errorDiv.Displayed, Is.True);
    }

    [Test]
    public void ValidatePassword_TooLongPassword_ShowsError()
    {
        // Arrange
        var passwordInput = _driver!.FindElement(By.Id("password"));
        var errorDiv = _driver.FindElement(By.Id("passwordError"));

        // Act
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('password').value = '" + new string('x', 129) + "';"
        );

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('password').dispatchEvent(new Event('input'));"
        );

        // Assert
        Assert.That(errorDiv.Displayed, Is.True);
    }


    [Test]
    public void ValidatePassword_ValidPassword_HidesError()
    {
        // Arrange
        var passwordInput = _driver!.FindElement(By.Id("password"));
        var errorDiv = _driver.FindElement(By.Id("passwordError"));

        // Act
        passwordInput.SendKeys("ValidPassword123");
        passwordInput.SendKeys(Keys.Tab); // Trigger onInput event

        // Assert
        Assert.That(errorDiv.Displayed, Is.False);
    }

    [Test]
    public void ValidateForm_InvalidPassword_PreventSubmission()
    {
        // Arrange
        var form = _driver!.FindElement(By.TagName("form"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var currentUrl = _driver.Url;

        // Act
        passwordInput.SendKeys("short");
        form.Submit();

        // Assert
        Assert.That(_driver.Url, Is.EqualTo(currentUrl));
    }
    
    [Test]
    public void ValidateForm_ValidPassword_AllowsSubmission()
    {
        // Arrange
        var passwordInput = _driver!.FindElement(By.Id("password"));
        var usernameInput = _driver.FindElement(By.Name("username"));

        // Act
        usernameInput.SendKeys("testuser");
        passwordInput.SendKeys("ValidPassword123");
        
        var isValid = (bool)((IJavaScriptExecutor)_driver).ExecuteScript(@"
        const password = document.getElementById('password');
        return validatePassword(password);
    ");

        // Assert
        Assert.That(isValid, Is.True, "Password validation should pass");
    }
}