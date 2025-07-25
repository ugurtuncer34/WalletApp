using AutoMapper;

namespace WalletBackend.Mapping;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountReadDto>()
            .ForMember(d => d.Currency,
                opt => opt.MapFrom(s => s.Currency.ToString()));  // "TRY" not 1
        // create/update dto -> entity (enum stays enum)
        CreateMap<AccountCreateDto, Account>();
        CreateMap<AccountUpdateDto, Account>();
    }
}