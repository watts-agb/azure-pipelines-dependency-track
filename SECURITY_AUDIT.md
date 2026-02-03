# Security Audit Report
**Date:** 2026-02-03  
**Auditor:** GitHub Copilot Security Agent  
**Repository:** watts-agb/azure-pipelines-dependency-track  
**Purpose:** Deep security investigation for forked repository handling sensitive organizational data

---

## Executive Summary

This repository is an Azure DevOps extension for integrating Dependency-Track SBOM (Software Bill of Materials) vulnerability scanning into CI/CD pipelines. A comprehensive security audit has been performed focusing on:

1. **Malicious Code Detection** - No backdoors or malicious patterns found
2. **Dependency Vulnerabilities** - Production dependencies are clean
3. **Code Security** - One potential security issue identified (URL encoding)
4. **Credential Handling** - Generally secure, no hardcoded secrets
5. **Network Communication** - All network calls go to user-configured Dependency-Track servers

**OVERALL RISK ASSESSMENT: LOW-MEDIUM**

The codebase appears to be legitimate with no evidence of malicious intent. However, one security issue should be addressed before production use.

---

## Detailed Findings

### ✅ 1. Malicious Code Analysis - PASSED

**Checked for:**
- Backdoors or hidden functionality
- Data exfiltration attempts
- Unauthorized network communications
- Code obfuscation
- Suspicious encoding/decoding operations

**Results:**
- ✅ No suspicious network calls to external domains
- ✅ No obfuscated or base64-encoded payloads
- ✅ No calls to `eval()`, `Function()`, or dynamic code execution
- ✅ No suspicious environment variable manipulation
- ✅ All network communication is to user-configured Dependency-Track servers only
- ✅ No hidden files or suspicious scripts outside of legitimate test infrastructure

**Code Review:**
- `dtrackClient.js`: Only communicates with the configured Dependency-Track server
- `task.js`: Standard Azure Pipelines task implementation
- Shell scripts are only for test environment setup (Docker Compose for Dependency-Track)

---

### ✅ 2. Dependency Security - PASSED (with minor dev dependency issue)

**Production Dependencies (shipped with extension):**
```json
{
  "axios": "^1.13.2",
  "azure-pipelines-task-lib": "^5.2.4",
  "form-data": "^4.0.5"
}
```

**GitHub Advisory Database Results:**
- ✅ **axios 1.13.2** - No known vulnerabilities
- ✅ **azure-pipelines-task-lib 5.2.4** - No known vulnerabilities  
- ✅ **form-data 4.0.5** - No known vulnerabilities
- ✅ **minimist 1.2.8** (root dev dependency) - No known vulnerabilities

**Dev Dependencies:**
- ⚠️ **glob 10.2.0-10.4.5** - Command injection vulnerability (GHSA-5j98-mcp5-4vw2)
  - **Severity:** HIGH
  - **Impact:** Only affects Jest test runner, NOT production code
  - **Risk:** LOW - Dev dependency only, not shipped in extension
  - **Recommendation:** Can be fixed with `npm audit fix` but not critical

**Package Sources:**
- All dependencies are from official npm registry (https://registry.npmjs.org/)
- No suspicious or private package registries detected

---

### ⚠️ 3. URL Encoding Vulnerability - REQUIRES ATTENTION

**Location:** `UploadBOM/src/dtrackClient.js` lines 73 and 90

**Issue:** Unencoded URL parameters in GET requests

```javascript
// Line 73 - Vulnerable
const response = await this.axiosInstance.get(
  `/api/v1/project/lookup?name=${projectName}&version=${projectVersion}`
);

// Line 90 - Vulnerable  
const response = await this.axiosInstance.get(
  `/api/v1/project?name=${projectName}`
);
```

**Vulnerability Type:** URL Parameter Injection / Improper Input Validation

**Attack Scenario:**
If a project name or version contains special characters like `&`, `=`, `?`, or `%`, this could:
1. Break the URL parsing
2. Inject additional query parameters
3. Cause unexpected API behavior or errors
4. Potentially bypass filters in edge cases

**Example:**
- Project name: `MyApp&malicious=true`
- Constructed URL: `/api/v1/project?name=MyApp&malicious=true`
- This adds an unintended `malicious` parameter

**Severity:** MEDIUM
- Not directly exploitable for code execution
- Could cause API errors or unexpected behavior
- May allow parameter pollution in edge cases

**Recommended Fix:**
```javascript
// Use encodeURIComponent for all URL parameters
const response = await this.axiosInstance.get(
  `/api/v1/project/lookup?name=${encodeURIComponent(projectName)}&version=${encodeURIComponent(projectVersion)}`
);
```

**Risk Level:** MEDIUM - Should be fixed before production use

---

### ✅ 4. Credential Handling - PASSED

**API Key Management:**
- ✅ API keys are retrieved from Azure Pipelines variables (secure)
- ✅ No hardcoded credentials found in source code
- ✅ Credentials are passed via HTTP headers (`X-API-Key`), not in URL
- ✅ No credentials logged to console (only operation token is logged, which is safe)

**Code Review:**
```javascript
// taskParametersUtility.js - Secure credential retrieval
dtrackAPIKey = tl.getEndpointAuthorizationParameter(serviceConnectionId, 'password', false);
// OR
dtrackAPIKey = tl.getInput('dtrackAPIKey', true);
```

**SSL/TLS:**
- ✅ Supports custom CA certificates for self-signed certificates
- ✅ HTTPS communication with Dependency-Track
- ✅ Certificate handling implemented correctly

---

### ✅ 5. Input Validation - MOSTLY PASSED

**File Operations:**
- ✅ File paths validated using Azure Pipelines task library
- ✅ BOM file validated as file before reading
- ✅ No path traversal vulnerabilities detected

**Parameter Validation:**
```javascript
// task.js - Proper validation
if (!tl.stats(path).isFile()) {
  throw new Error(localize('FileNotFound', path));
}
```

**Injection Prevention:**
- ✅ SQL Injection: N/A (no database queries)
- ✅ Command Injection: No shell commands with user input
- ✅ XML/XXE: BOM file passed as-is to API (validated by Dependency-Track)
- ⚠️ URL Encoding: Issue identified (see section 3)

---

### ✅ 6. Error Handling - PASSED

**Information Disclosure:**
- ✅ Error messages don't expose sensitive data
- ✅ API errors are properly wrapped
- ✅ Stack traces controlled by Azure Pipelines framework

**Error Logging:**
```javascript
// utils.js - Safe error formatting
static getErrorMessage(err){
  if (err.response) {
    return `${err.response.status} - ${err.response.statusText}`;
  }
  // No sensitive data in error messages
}
```

---

### ✅ 7. Code Quality & Maintainability - GOOD

**Observations:**
- ✅ Clean, readable code structure
- ✅ Separation of concerns (client, manager, task)
- ✅ Comprehensive test coverage (unit & integration tests)
- ✅ Uses standard Azure Pipelines task library
- ✅ Well-documented README with security permissions

**Test Infrastructure:**
- Docker-based integration tests with Dependency-Track
- Least-privilege API key testing
- No malicious test code detected

---

## Additional Security Considerations

### 1. Third-Party API Communication
The extension communicates with Dependency-Track, which is under your organization's control. Ensure:
- Dependency-Track server is properly secured
- Network communication is encrypted (HTTPS)
- API keys have minimal required permissions
- Regular security updates for Dependency-Track itself

### 2. Azure DevOps Permissions
The extension requests `vso.build_execute` scope, which is appropriate for build tasks. No excessive permissions requested.

### 3. Build Output
The extension processes SBOM files (Software Bill of Materials). Ensure:
- SBOM files don't contain sensitive information
- Build artifacts are properly secured
- Dependency-Track access is restricted

---

## Recommendations

### CRITICAL (Fix Before Production)
❌ None - No critical security issues found

### HIGH PRIORITY
1. ⚠️ **Fix URL Encoding Vulnerability** in `dtrackClient.js`
   - Use `encodeURIComponent()` for all URL parameters
   - Add input validation for project names/versions
   - Add test cases for special characters

### MEDIUM PRIORITY
2. ✅ **Update Dev Dependencies** (Optional)
   - Run `npm audit fix` to resolve glob vulnerability
   - Consider updating deprecated packages (q, uuid)

### LOW PRIORITY
3. ✅ **Code Enhancements** (Optional)
   - Add rate limiting for API calls
   - Implement request timeout configuration
   - Add more detailed error messages

### BEST PRACTICES
4. ✅ **Operational Security**
   - Use Azure DevOps service connections for API keys (not variables)
   - Follow principle of least privilege for Dependency-Track API keys
   - Regularly rotate API keys
   - Monitor Dependency-Track logs for suspicious activity
   - Keep extension and dependencies updated

---

## Testing Performed

1. ✅ Static code analysis of all JavaScript source files
2. ✅ Dependency vulnerability scanning (GitHub Advisory Database)
3. ✅ Pattern matching for malicious code indicators
4. ✅ Network communication analysis
5. ✅ Credential handling review
6. ✅ Input validation testing
7. ✅ File operation security review
8. ✅ Review of test infrastructure

---

## Conclusion

**VERDICT: SAFE TO USE WITH MINOR FIX**

This repository appears to be a legitimate Azure DevOps extension with no evidence of malicious code or backdoors. The code is well-structured, properly tested, and follows security best practices for the most part.

**Before Production Deployment:**
1. ✅ Fix the URL encoding vulnerability (HIGH priority)
2. ✅ Optionally update dev dependencies
3. ✅ Configure proper API key permissions in Dependency-Track
4. ✅ Use Azure DevOps service connections for secure credential management

**Risk Level:** LOW-MEDIUM
- **LOW** risk for malicious code or backdoors (none found)
- **MEDIUM** risk due to URL encoding issue (should be fixed)

**Approval Status:** ✅ APPROVED with recommended fixes

The original fork source (github.com/Zargath/azure-pipelines-dependency-track) appears to be maintained by Edouard Shaar, and the code quality and security practices are professional.

---

## Appendix: Files Reviewed

### Source Code (748 lines total)
- `UploadBOM/src/task.js` (128 lines)
- `UploadBOM/src/dtrackClient.js` (224 lines)
- `UploadBOM/src/dtrackManager.js` (180 lines)
- `UploadBOM/src/taskParametersUtility.js` (73 lines)
- `UploadBOM/src/thresholdExpert.js` (109 lines)
- `UploadBOM/src/utils.js` (25 lines)
- `UploadBOM/src/localization.js` (9 lines)

### Configuration Files
- `package.json` (root and UploadBOM)
- `vss-extension.json`
- `.github/workflows/dev.yml`
- `.github/workflows/prod.yml`

### Build & Test Scripts
- `version-bump.js`
- `UploadBOM/__tests__/integration/setup/dtrack-environment.sh`
- `UploadBOM/__tests__/integration/setup/generate-certs.sh`

### Documentation
- `README.md`
- `LICENSE`

---

**Audit Complete**
