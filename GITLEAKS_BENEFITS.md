# Gitleaks - Secret Scanning Tool

## What is Gitleaks?

Gitleaks is a SAST (Static Application Security Testing) tool that scans your code and git history for hardcoded secrets and credentials. It's designed to prevent secrets from being committed to your repository.

## Benefits of Using Gitleaks

### 1. **Comprehensive Secret Detection**
- Detects **100+ types of secrets** including:
  - API keys (AWS, Azure, Google Cloud, Stripe, etc.)
  - Passwords and credentials
  - Private keys (RSA, SSH, PGP)
  - OAuth tokens
  - Database connection strings
  - JWT secrets
  - GitHub tokens
  - And many more...

### 2. **Git History Scanning**
- Scans **entire repository history**, not just current code
- Detects secrets that were committed in the past
- Can identify secrets in old commits that need to be rotated
- Prevents accidental exposure through git history

### 3. **Fast and Efficient**
- Lightweight and fast scanning
- Low false positive rate
- Minimal impact on pipeline execution time
- Scans entire repository in seconds

### 4. **Prevents Security Breaches**
- **Stops secrets before they're committed** (if used as pre-commit hook)
- Fails the build if secrets are detected
- Prevents secrets from being merged into main branch
- Protects against accidental credential exposure

### 5. **Compliance and Best Practices**
- Helps meet security compliance requirements
- Enforces security best practices
- Provides audit trail of secret scanning
- Generates SARIF reports for security dashboards

### 6. **Integration with Azure DevOps**
- Native Azure DevOps task extension
- Results appear in Security tab
- SARIF format for detailed reporting
- Can be configured to fail builds on secrets

## How It Works in Your Pipeline

### Current Configuration:
```yaml
- task: Gitleaks@8
  inputs:
    scanMode: 'repository'        # Scans entire repository
    reportFormat: 'sarif'          # Generates SARIF report
    failOnSecrets: true           # Fails build if secrets found
    severityThreshold: 'high'      # Only fails on high severity
```

### What It Does:
1. **Scans Repository**: Analyzes all files in the repository
2. **Detects Secrets**: Uses pattern matching to find potential secrets
3. **Generates Report**: Creates SARIF report with findings
4. **Fails Build**: If secrets are detected, build fails (prevents merge)
5. **Publishes Results**: Results appear in Azure DevOps Security tab

## Example Output

```
🔒 Gitleaks - Secret Scanning
==========================================
Scanning repository for secrets...

❌ SECRETS DETECTED:
  File: src/Service.cs
  Line: 45
  Secret Type: AWS Access Key
  Severity: High
  Match: AK...

  File: appsettings.json
  Line: 12
  Secret Type: API Key
  Severity: High
  Match: 51Hqj....

🚨 BUILD FAILED: Secrets detected in code!
```

## Comparison with Other Tools

| Feature | Gitleaks | Custom Regex Scan | GitHub Advanced Security |
|---------|----------|-------------------|------------------------|
| Secret Detection | ✅ 100+ types | ⚠️ Limited patterns | ✅ Comprehensive |
| Git History Scan | ✅ Yes | ❌ No | ✅ Yes |
| Speed | ✅ Fast | ✅ Fast | ⚠️ Slower |
| False Positives | ✅ Low | ⚠️ Medium | ✅ Low |
| Cost | ✅ Free | ✅ Free | ⚠️ Paid (GitHub) |
| Azure DevOps Integration | ✅ Native | ⚠️ Custom | ⚠️ Requires Extension |

## Best Practices

1. **Use with Pre-commit Hooks**: Catch secrets before they're committed
2. **Fail on High Severity**: Configure to fail builds on critical secrets
3. **Review Findings**: Not all findings are actual secrets (false positives)
4. **Rotate Exposed Secrets**: If secrets are found, rotate them immediately
5. **Use Secret Management**: Store secrets in Azure Key Vault or similar

## What to Do If Secrets Are Found

1. **Immediately Rotate the Secret**: Change the exposed credential
2. **Remove from Code**: Delete the hardcoded secret
3. **Use Secret Management**: Store in Azure Key Vault, User Secrets, or Environment Variables
4. **Clean Git History**: If secret was in history, consider using `git filter-branch` or BFG Repo-Cleaner
5. **Review Access Logs**: Check if the secret was accessed by unauthorized parties

## Configuration Options

### Scan Modes:
- `repository` - Scans entire repository (recommended)
- `commit` - Scans specific commit
- `staged` - Scans staged files only

### Severity Levels:
- `critical` - Only critical secrets fail build
- `high` - High and critical secrets fail build (recommended)
- `medium` - Medium, high, and critical fail build
- `low` - All secrets fail build

### Report Formats:
- `sarif` - For Azure DevOps Security tab
- `json` - For custom processing
- `csv` - For spreadsheet analysis

## Integration with Your Pipeline

Gitleaks is now integrated in your pipeline and will:
- ✅ Run automatically on every PR
- ✅ Fail the build if secrets are detected
- ✅ Publish results to Security tab
- ✅ Work alongside your existing security scans

This provides **defense in depth** - multiple layers of security scanning to catch secrets before they reach production.

