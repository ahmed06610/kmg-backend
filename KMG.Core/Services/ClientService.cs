using KMG.Core.DTOs.Client;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Core.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static IQueryable<Client> IncludeAll(IQueryable<Client> query) =>
            query.Include(c => c.Projects).ThenInclude(p => p.Payments);

        public async Task<List<ClientListDTO>> GetAllAsync()
        {
            var clients = await IncludeAll(_unitOfWork.Client.GetQueryable(null)).ToListAsync();
            return clients.Select(MapList).ToList();
        }

        public async Task<ClientDetailsDTO?> GetByIdAsync(int id)
        {
            var client = await IncludeAll(_unitOfWork.Client.GetQueryable(c => c.Id == id))
                .Include(c => c.Projects).ThenInclude(p => p.StockMovements)
                .Include(c => c.Projects).ThenInclude(p => p.Expenses)
                .Include(c => c.Projects).ThenInclude(p => p.Missions).ThenInclude(m => m.MissionWorkers).ThenInclude(w => w.Employee)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (client == null) return null;

            var dto = new ClientDetailsDTO
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                Email = client.Email,
                Address = client.Address,
                CreatedAt = client.CreatedAt,
                ProjectsCount = client.Projects.Count,
                TotalContractValue = client.Projects.Sum(p => p.ContractValue),
                TotalCollected = client.Projects.Sum(p => p.TotalCollected),
                TotalRemaining = client.Projects.Sum(p => p.RemainingBalance),
                Projects = client.Projects.Select(p => new ClientProjectSummaryDTO
                {
                    Id = p.Id,
                    ProjectCode = p.ProjectCode,
                    ProjectType = p.ProjectType.ToString(),
                    Status = p.Status.ToString(),
                    ContractValue = p.ContractValue,
                    TotalCollected = p.TotalCollected,
                    RemainingBalance = p.RemainingBalance
                }).ToList()
            };

            return dto;
        }

        public async Task<int> CreateAsync(CreateClientDTO model)
        {
            var client = new Client
            {
                Name = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                CreatedAt = TimeHelper.NowInEgypt
            };

            await _unitOfWork.Client.AddAsync(client);
            await _unitOfWork.CompleteAsync();
            return client.Id;
        }

        public async Task<bool> UpdateAsync(UpdateClientDTO model)
        {
            var client = await _unitOfWork.Client.GetByIdAsync(model.Id);
            if (client == null) return false;

            client.Name = model.Name;
            client.Phone = model.Phone;
            client.Email = model.Email;
            client.Address = model.Address;

            _unitOfWork.Client.Update(client);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        private static ClientListDTO MapList(Client c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            ProjectsCount = c.Projects.Count,
            TotalContractValue = c.Projects.Sum(p => p.ContractValue),
            TotalCollected = c.Projects.Sum(p => p.TotalCollected),
            TotalRemaining = c.Projects.Sum(p => p.RemainingBalance)
        };
    }
}
