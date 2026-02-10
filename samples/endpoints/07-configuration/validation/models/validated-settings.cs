using System.ComponentModel.DataAnnotations;

public class ValidatedSettings
{
  [Required(ErrorMessage = "ApiKey is required")]
  [StringLength(100, MinimumLength = 10, ErrorMessage = "ApiKey must be 10-100 characters")]
  public string ApiKey { get; set; } = "default-key-12345";

  [Range(100, 60000, ErrorMessage = "Timeout must be between 100ms and 60000ms")]
  public int TimeoutMs { get; set; } = 5000;

  [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
  public int MaxRetries { get; set; } = 3;

  [Required]
  [Url(ErrorMessage = "EndpointUrl must be a valid URL")]
  public string EndpointUrl { get; set; } = "https://api.example.com";

  [Required]
  [AllowedValues("Development", "Staging", "Production")]
  public string Environment { get; set; } = "Development";

  [MaxLength(5, ErrorMessage = "Maximum 5 tags allowed")]
  public string[] Tags { get; set; } = ["cli", "api"];
}
