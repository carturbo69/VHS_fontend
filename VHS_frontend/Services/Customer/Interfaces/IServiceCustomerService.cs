using VHS_frontend.Areas.Admin.Models.Category;
using VHS_frontend.Models.ServiceDTOs;

namespace VHS_frontend.Services.Customer.Interfaces
{
    public interface IServiceCustomerService
    {
        Task<IEnumerable<ListServiceHomePageDTOs>> GetAllServiceHomePageAsync();

        Task<List<CategoryDTO>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default);


        Task<IEnumerable<ListServiceHomePageDTOs>> GetTop05HighestRatedServicesAsync();
        Task<ServiceDetailDTOs?> GetServiceDetailAsync(Guid serviceId);
    }
}
