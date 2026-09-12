using ProzorroDataMining.Core.Entities;
using ProzorroDataMining.Core.Entities.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Mapper
{
    public class TenderMapper
    {
        public TenderImportModel Map(TenderResponseDto response)
        {
            if (response == null || response.Data == null)
            {
                return null;
            }

            var data = response.Data;

            var contractTotal = 0m;

            if (data.Contracts != null)
            {
                foreach (var contract in data.Contracts)
                {
                    if (contract != null &&
                        contract.Value != null &&
                        contract.Value.Amount.HasValue)
                    {
                        contractTotal += contract.Value.Amount.Value;
                    }
                }
            }

            decimal? savings = null;

            if (data.Value != null &&
                data.Value.Amount.HasValue)
            {
                savings = data.Value.Amount.Value - contractTotal;
            }

            var cpvCode = GetCpvCode(data);
            var suppliers = GetSupplierNames(data);

            return new TenderImportModel
            {
                ExternalId = data.Id,
                CPVCode = cpvCode,
                Status = data.Status,
                ProcuringEntityName =
                    data.ProcuringEntity == null
                        ? null
                        : data.ProcuringEntity.Name,
                StartingAmount =
                    data.Value == null
                        ? null
                        : data.Value.Amount,
                ContractTotal = contractTotal,
                Savings = savings,
                BusinessOrganisationNames = suppliers
            };
        }

        private string GetCpvCode(TenderDataDto data)
        {
            if (data.Items == null)
            {
                return null;
            }

            foreach (var item in data.Items)
            {
                if (item != null &&
                    item.Classification != null &&
                    !string.IsNullOrEmpty(item.Classification.Id))
                {
                    return item.Classification.Id;
                }
            }

            return null;
        }

        private IReadOnlyCollection<string> GetSupplierNames(
            TenderDataDto data)
        {
            var result = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            if (data.Awards == null)
            {
                return result;
            }

            foreach (var award in data.Awards)
            {
                if (award == null || award.Suppliers == null)
                {
                    continue;
                }

                foreach (var supplier in award.Suppliers)
                {
                    if (supplier != null &&
                        !string.IsNullOrWhiteSpace(supplier.Name))
                    {
                        result.Add(supplier.Name);
                    }
                }
            }

            return result;
        }
    }
}
