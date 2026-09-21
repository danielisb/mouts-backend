using Ambev.DeveloperEvaluation.Common.Security;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

public class JwtTokenGeneratorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateToken_ShouldRejectMissingSecret(string? secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = secret })
            .Build();
        var generator = new JwtTokenGenerator(configuration);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            generator.GenerateToken(Substitute.For<IUser>()));

        Assert.Equal("JWT secret key is not configured.", exception.Message);
    }
}
