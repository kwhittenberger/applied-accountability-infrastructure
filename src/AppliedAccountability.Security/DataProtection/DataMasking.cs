using System.Text;
using System.Text.RegularExpressions;

namespace AppliedAccountability.Security.DataProtection;

/// <summary>
/// Utility class for masking sensitive data
/// </summary>
public static class DataMasking
{
    /// <summary>
    /// Masks an email address (e.g., "user@example.com" -> "u***@example.com")
    /// </summary>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return email;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return email;

        var username = parts[0];
        var domain = parts[1];

        var maskedUsername = username.Length > 1
            ? username[0] + new string('*', Math.Min(username.Length - 1, 3))
            : username;

        return $"{maskedUsername}@{domain}";
    }

    /// <summary>
    /// Masks a phone number (e.g., "555-123-4567" -> "***-***-4567")
    /// </summary>
    public static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Keep only the last 4 digits visible
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return new string('*', phoneNumber.Length);

        var visibleDigits = digits[^4..];
        var maskedLength = phoneNumber.Length - 4;

        return new string('*', maskedLength) + visibleDigits;
    }

    /// <summary>
    /// Masks a credit card number (e.g., "4111-1111-1111-1111" -> "****-****-****-1111")
    /// </summary>
    public static string MaskCreditCard(string creditCard)
    {
        if (string.IsNullOrWhiteSpace(creditCard))
            return creditCard;

        // Keep only the last 4 digits visible
        var digits = new string(creditCard.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return new string('*', creditCard.Length);

        var visibleDigits = digits[^4..];
        var format = creditCard.Replace(digits, new string('*', digits.Length));

        // Replace the last 4 asterisks with actual digits
        var sb = new StringBuilder(format);
        var digitIndex = visibleDigits.Length - 1;

        for (var i = sb.Length - 1; i >= 0 && digitIndex >= 0; i--)
        {
            if (sb[i] == '*')
            {
                sb[i] = visibleDigits[digitIndex--];
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Masks a Social Security Number (e.g., "123-45-6789" -> "***-**-6789")
    /// </summary>
    public static string MaskSsn(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn))
            return ssn;

        var digits = new string(ssn.Where(char.IsDigit).ToArray());
        if (digits.Length != 9)
            return new string('*', ssn.Length);

        return $"***-**-{digits[^4..]}";
    }

    /// <summary>
    /// Redacts personally identifiable information (PII) from text
    /// </summary>
    public static string RedactPii(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = text;

        // Redact email addresses
        result = Regex.Replace(result, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
            match => MaskEmail(match.Value));

        // Redact phone numbers
        result = Regex.Replace(result, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b",
            match => MaskPhoneNumber(match.Value));

        // Redact credit card numbers
        result = Regex.Replace(result, @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b",
            match => MaskCreditCard(match.Value));

        // Redact SSN
        result = Regex.Replace(result, @"\b\d{3}-\d{2}-\d{4}\b",
            match => MaskSsn(match.Value));

        return result;
    }

    /// <summary>
    /// Masks a string by showing only the first and last N characters
    /// </summary>
    public static string MaskMiddle(string value, int visibleChars = 2)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value.Length <= visibleChars * 2)
            return new string('*', value.Length);

        var start = value[..visibleChars];
        var end = value[^visibleChars..];
        var maskLength = value.Length - (visibleChars * 2);

        return $"{start}{new string('*', maskLength)}{end}";
    }

    /// <summary>
    /// Completely masks a value with asterisks
    /// </summary>
    public static string MaskComplete(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return new string('*', value.Length);
    }
}
