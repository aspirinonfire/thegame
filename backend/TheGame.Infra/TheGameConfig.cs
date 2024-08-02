using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TheGame.Infra;

public sealed record TheGameConfig
{
  public const string GhcrPatSecretName = "ghcr-pat";
  public const string GoogleClientSecretName = "google-client-secret";
  public const string JwtSecretName = "jwt-secret";

  // Pulumi Data
  [Required]
  public string ProjectName { get; set; } = default!;
  [Required]
  public string StackName { get; set; } = default!;
  [Required]
  public string AzureNativeVersion { get; set; } = default!;
  [Required]
  public string AzureAdVersion { get; set; } = default!;
  [Required]
  [RegularExpression("^azblob://\\w+\\?storage_account=\\w+$", ErrorMessage = "Backend URL must match Azure Blob Storage Format: azblob://<container_name>?storage_account=<azure_storage_acc_name>", MatchTimeoutInMilliseconds = 1000)]
  public string BackendBlobStorageUrl { get; set; } = default!;

  // Azure Environment
  [Required]
  public string SubscriptionId { get; set; } = default!;
  
  [Required]
  public string ResourceGroupName { get; set; } = default!;

  // Azure SQL
  [Required]
  public string DbServerName { get; set; } = default!;
  [Required]
  public string DbName { get; set; } = default!;
  
  public string DbSku { get; set; } = default!;

  // Azure Container Apps
  [Required]
  public string AcaEnvName { get; set; } = default!;
  [Required]
  public string AcaName { get; set; } = default!;
  [Required]
  public string GhcrUrl { get; set; } = default!;
  [Required]
  public string GhcrUsername { get; set; } = default!;
  [Required]
  public string GhcrPat { get; set; } = default!;
  [Required]
  public string GameImage { get; set; } = default!;

  // Game Auth
  [Required]
  public string GoogleClientId { get; set; } = default!;
  
  [Required]
  public string GoogleClientSecret { get; set; } = default!;
  
  [Required]
  [MinLength(32, ErrorMessage = "JWT secret must be at least 32 chars long")]
  public string JwtSecret { get; set; } = default!;
  
  [Required]
  public string JwtAudience { get; set; } = default!;
  
  [Range(1,  60 * 24, ErrorMessage = "JWT expiration must be between 1 min and 1 day")]
  public int JwtTokenExpirationMin { get; set; }

  public IReadOnlyCollection<string> GetValidationErrors()
  {
    var validationContext = new ValidationContext(this);
    var validationResults = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);
    if (isValid)
    {
      return Array.Empty<string>();
    }

    return validationResults
      .Select(error => $"{string.Join(", ", error.MemberNames)}: {error.ErrorMessage ?? "n/a"}")
      .ToList()
      .AsReadOnly();
  }
}