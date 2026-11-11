using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Bll.Mapping;

namespace Orders.Tests;

public class MappingTests
{
    [Fact]
    public void AutoMapper_configuration_is_valid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderProfile>();
            cfg.AddProfile<CustomerProfile>();
            cfg.AddProfile<ProductProfile>();
        }, NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
