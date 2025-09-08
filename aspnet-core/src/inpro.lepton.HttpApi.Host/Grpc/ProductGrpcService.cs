using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using inpro.lepton.Grpc;
using Microsoft.Extensions.Logging;

namespace inpro.lepton;

public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly ILogger<ProductGrpcService> _logger;

    // TODO: сюди можна заінжектити твій AppService/репозиторій ABP для БД
    public ProductGrpcService(ILogger<ProductGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<ProductListReply> ListProducts(ProductListRequest request, ServerCallContext context)
    {
        // Демодані (заміниш на реальні)
        var all = new[]
        {
            new ProductDto { Id = "1", Name = "Brake Disc",      Price = 120.0 },
            new ProductDto { Id = "2", Name = "Control Arm",     Price = 85.5  },
            new ProductDto { Id = "3", Name = "Tie Rod",         Price = 40.0  },
            new ProductDto { Id = "4", Name = "Shock Absorber",  Price = 220.0 },
            new ProductDto { Id = "5", Name = "Stabilizer Link", Price = 25.0  },
        };

        // Сортування нечутливе до регістру
        var sortByName = !string.IsNullOrWhiteSpace(request.Sorting) &&
                         request.Sorting.Contains("name", StringComparison.OrdinalIgnoreCase);

        var sorted = sortByName ? all.OrderBy(p => p.Name).ToArray() : all;

        // Пагінація (увага: у C# властивості PascalCase)
        var skip = Math.Max(0, request.SkipCount);
        var take = request.MaxResultCount > 0 ? request.MaxResultCount : 10;

        var items = sorted.Skip(skip).Take(take).ToArray();

        var reply = new ProductListReply { TotalCount = all.Length };
        reply.Items.AddRange(items);

        _logger.LogInformation("Returned {Count} products (skip={Skip}, take={Take}, sorting='{Sorting}')",
            reply.Items.Count, skip, take, request.Sorting);

        return Task.FromResult(reply);
    }
}