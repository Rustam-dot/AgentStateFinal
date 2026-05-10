namespace AgencyRealEstate.WebUI.Services;

public class TokenStorage
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    
    private string? _inMemoryToken;

    public TokenStorage(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Token
    {
        get
        {
           
            if (!string.IsNullOrWhiteSpace(_inMemoryToken))
            {
                return _inMemoryToken;
            }

           
            return _httpContextAccessor.HttpContext?.Request.Cookies["authToken"];
        }
        set
        {
            
            _inMemoryToken = value;
        }
    }

    public void Clear()
    {
      
        _inMemoryToken = null;
    }
}