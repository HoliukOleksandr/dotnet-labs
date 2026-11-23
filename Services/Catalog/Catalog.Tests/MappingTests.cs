using AutoMapper;
using Catalog.Bll.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.Tests;

public class MappingTests
{
    [Fact]
    public void AutoMapper_configuration_is_valid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<CategoryProfile>();
        }, NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
