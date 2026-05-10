using AgencyRealEstate.WebUI.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AgencyRealEstate.WebUI.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> UploadAvatarAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 5_000_000));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);
        var response = await _http.PostAsync("profile/avatar", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AvatarResponse>();
        return result?.AvatarUrl ?? "";
    }

    public class AvatarResponse
    {
        public string AvatarUrl { get; set; } = "";
    }


    public async Task<List<PropertyDto>> GetPropertiesAsync()
    {
        return await _http.GetFromJsonAsync<List<PropertyDto>>("properties");
        
    }

        public async Task<PropertyDto> GetPropertyAsync(int id)
        {
            return await _http.GetFromJsonAsync<PropertyDto>($"properties/{id}");
        }
    public async Task CreateShowingAsync(CreateShowingRequest request)
    {
        var response = await _http.PostAsJsonAsync("showings", request);
        response.EnsureSuccessStatusCode();
    }
    public async Task<ProfileDto> GetProfileAsync()
    {
        return await _http.GetFromJsonAsync<ProfileDto>("profile");
    }

    public async Task UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await _http.PutAsJsonAsync("profile", request);
        response.EnsureSuccessStatusCode();
    }

}