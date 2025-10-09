using VHS_frontend.Models.ServiceDTOs;

namespace VHS_frontend.Services.Customer.Interfaces
{
    public interface IServiceCustomerService
    {
        Task<IEnumerable<ListServiceHomePageDTOs>> GetAllServiceHomePageAsync();
    }
}
