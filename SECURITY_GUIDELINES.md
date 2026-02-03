# Security Guidelines for Maintaining This Repository

**Purpose:** Protect your organization from malicious code in open-source dependencies and forks

---

## 🛡️ Defense-in-Depth Strategy

No single measure is 100% effective. Use multiple layers of security:

### 1. Fork Management Strategy

**✅ RECOMMENDED: Use a Private Fork**

```
Original Repo: github.com/Zargath/azure-pipelines-dependency-track (public)
        ↓ (fork)
Your Fork: github.com/watts-agb/azure-pipelines-dependency-track (PRIVATE)
        ↓ (deploy from)
Your Production: Azure DevOps environment
```

**Benefits:**
- You control all changes before they reach production
- No one can see your customizations or configurations
- You can audit upstream changes before merging
- Attackers can't see what version you're running

**How to Make Your Fork Private:**
1. Go to repository Settings
2. Scroll to "Danger Zone"
3. Click "Change visibility" → "Make private"

---

### 2. Upstream Change Management

**DO NOT auto-merge from upstream!** Always review changes manually.

#### Process for Syncing with Upstream:

```bash
# 1. Add upstream remote (one-time setup)
git remote add upstream https://github.com/Zargath/azure-pipelines-dependency-track.git

# 2. Fetch upstream changes
git fetch upstream

# 3. Create a review branch
git checkout -b review-upstream-changes
git merge upstream/main --no-commit --no-ff

# 4. REVIEW ALL CHANGES before committing
git diff --cached

# 5. Run security audit (see section below)

# 6. Only merge if safe
git commit -m "Reviewed and merged upstream changes"
git checkout main
git merge review-upstream-changes
```

#### What to Look For in Upstream Changes:

🚨 **RED FLAGS** - Reject immediately:
- New network calls to external domains
- Base64 encoded strings (especially in new code)
- `eval()`, `Function()`, or dynamic code execution
- New dependencies from unknown sources
- Code obfuscation or minified files in source
- Changes to credential handling
- New file system operations outside expected paths
- Suspicious environment variable access

⚠️ **YELLOW FLAGS** - Investigate carefully:
- New npm dependencies (check each one)
- Changes to network/HTTP code
- Changes to authentication/authorization
- New shell script execution
- Changes to build/deployment scripts
- Updates to GitHub Actions workflows

✅ **GREEN FLAGS** - Generally safe:
- Bug fixes in existing code
- Documentation updates
- Test improvements
- Performance optimizations (review code)
- Dependency version bumps (check each dependency)

---

### 3. Dependency Security Monitoring

#### A. Enable Dependabot (Automated Scanning)

**Setup (in GitHub):**
1. Go to repository Settings → Security
2. Enable "Dependabot alerts"
3. Enable "Dependabot security updates"

**Create `.github/dependabot.yml`:**
```yaml
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/UploadBOM"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
  
  - package-ecosystem: "npm"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
  
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

#### B. Regular Dependency Audits

**Run monthly (minimum):**
```bash
# Check for known vulnerabilities
cd UploadBOM
npm audit

# Check for outdated packages
npm outdated

# Review each dependency
npm list --depth=0
```

**Review each new dependency:**
```bash
# Before adding any new dependency:
# 1. Check npm package page
npm view <package-name>

# 2. Check GitHub repository
# - Is it actively maintained?
# - How many stars/downloads?
# - Who are the maintainers?
# - Recent commits?

# 3. Check for security issues
# - Search: "npm <package-name> vulnerability"
# - Check: https://security.snyk.io/package/npm/<package-name>
```

---

### 4. Code Review Checklist

**Before merging ANY changes (internal or upstream):**

```
□ Code review completed by at least one person
□ All tests pass
□ No new security warnings
□ Dependencies scanned for vulnerabilities
□ No suspicious network calls added
□ No new credentials or secrets in code
□ Changes are minimal and well-documented
□ Build artifacts verified (no unexpected files)
```

---

### 5. Automated Security Scanning

#### A. Enable GitHub Code Scanning (CodeQL)

**Create `.github/workflows/security.yml`:**
```yaml
name: Security Scan

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * 1'  # Weekly on Monday

jobs:
  codeql:
    name: CodeQL Analysis
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      actions: read
      contents: read
    
    steps:
    - name: Checkout repository
      uses: actions/checkout@v4
    
    - name: Initialize CodeQL
      uses: github/codeql-action/init@v3
      with:
        languages: javascript
    
    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v3
```

#### B. Add npm Audit to CI Pipeline

**Add to `.github/workflows/dev.yml` and `prod.yml`:**
```yaml
    - name: Security Audit
      run: |
        cd UploadBOM
        npm audit --audit-level=high
      continue-on-error: false
```

---

### 6. Trust and Verification

#### Should You Trust the Original Author?

**Current Status:**
- Author: Edouard Shaar (@Zargath)
- Repository: Professional code quality
- Marketplace: Published extension with users
- Assessment: Appears trustworthy

**BUT - Trust Should Be Verified:**
- ✅ Code quality is good (verified in audit)
- ✅ No malicious patterns found (verified in audit)
- ⚠️ You should still review all future changes
- ⚠️ People can be compromised (account takeovers happen)
- ⚠️ Maintainers can change

**"Trust but Verify" Approach:**
1. The original author appears legitimate
2. BUT always review changes before merging
3. Never auto-merge from upstream
4. Use automated tools to catch issues you might miss

---

### 7. Access Control and Branch Protection

#### A. Enable Branch Protection

**Configure for `main` branch:**
1. Go to Settings → Branches → Add rule
2. Branch name pattern: `main`
3. Enable:
   - ✅ Require pull request before merging
   - ✅ Require approvals (at least 1)
   - ✅ Require status checks to pass
   - ✅ Require conversation resolution
   - ✅ Do not allow bypassing (even admins)

#### B. Limit Repository Access

- Only grant write access to trusted team members
- Use "Read" access for most users
- Review access list quarterly
- Remove access for departed team members immediately

---

### 8. Incident Response Plan

**If you discover malicious code:**

1. **Immediate Actions:**
   ```bash
   # Stop using the extension immediately
   # Revoke all API keys used by the extension
   # Rollback to last known-good version
   ```

2. **Investigation:**
   - Identify what the malicious code does
   - Check logs for evidence of exploitation
   - Determine scope of impact

3. **Remediation:**
   - Remove malicious code
   - Update all secrets/credentials
   - Notify security team
   - Document incident

4. **Prevention:**
   - Review how malicious code was introduced
   - Update security processes
   - Train team on lessons learned

---

### 9. Regular Security Reviews

**Schedule:**
- **Weekly:** Check Dependabot alerts
- **Monthly:** Review dependency security
- **Quarterly:** Full code audit of new changes
- **Yearly:** Complete security assessment

**Quarterly Review Checklist:**
```
□ Review all changes since last audit
□ Check for new dependencies
□ Run npm audit
□ Review GitHub security alerts
□ Verify branch protection rules
□ Review access control list
□ Test extension functionality
□ Review logs for anomalies
```

---

### 10. Production Deployment Best Practices

#### A. Staged Deployment

```
Dev Environment → Test Environment → Production
     ↓                  ↓                 ↓
  (Test here)      (Validate here)  (Deploy here)
```

#### B. Monitoring

- Monitor Dependency-Track API access logs
- Alert on unusual API patterns
- Track which projects are being accessed
- Monitor for data exfiltration attempts

#### C. Least Privilege

- Use separate API keys for different environments
- Limit API key permissions to minimum required
- Rotate API keys quarterly
- Never use admin API keys for CI/CD

---

## 🎯 Recommended Security Posture for Your Use Case

Given that you'll "implement and forget" this in daily pipelines:

### CRITICAL - Must Do:

1. ✅ **Make your fork private**
2. ✅ **Enable Dependabot alerts**
3. ✅ **Enable branch protection**
4. ✅ **Add CodeQL scanning to GitHub Actions**
5. ✅ **Set up quarterly security review calendar reminder**

### HIGH PRIORITY - Should Do:

6. ✅ **Create review process for upstream changes**
7. ✅ **Add npm audit to CI pipeline**
8. ✅ **Document who has repository access**
9. ✅ **Set up monitoring for Dependency-Track logs**

### RECOMMENDED - Nice to Have:

10. ✅ **Implement staged deployment**
11. ✅ **Add security scanning tools (Snyk, etc.)**
12. ✅ **Create security incident response plan**

---

## 📋 Quick Reference: Security Audit Checklist

**Use this checklist when reviewing ANY changes:**

```
BEFORE MERGING:
□ Run: npm audit (no high/critical vulnerabilities)
□ Review: All changed files manually
□ Check: No new external network calls
□ Verify: No new dependencies (or all reviewed)
□ Confirm: No hardcoded secrets/credentials
□ Test: All tests pass
□ Validate: Extension works as expected

RED FLAGS (REJECT):
□ Base64 encoding of strings
□ eval() or Function() calls
□ Obfuscated code
□ Calls to unknown external APIs
□ New dependencies from suspicious sources
□ Changes to credential handling

AFTER MERGING:
□ Monitor: First deployment closely
□ Check: Dependency-Track logs for anomalies
□ Verify: No unexpected behavior
```

---

## 🔗 Additional Resources

- **npm Security Best Practices:** https://docs.npmjs.com/security-best-practices
- **GitHub Security Features:** https://docs.github.com/en/code-security
- **OWASP Dependency Check:** https://owasp.org/www-project-dependency-check/
- **Supply Chain Security:** https://slsa.dev/

---

## Summary: Your Action Plan

1. **Immediate (This Week):**
   - Make fork private
   - Enable Dependabot
   - Set up branch protection
   - Add CodeQL scanning

2. **Short-term (This Month):**
   - Document upstream change review process
   - Add security scanning to CI pipeline
   - Create calendar reminder for quarterly reviews

3. **Ongoing:**
   - Review all changes before merging
   - Check Dependabot alerts weekly
   - Never auto-merge from upstream
   - Maintain "trust but verify" mindset

**Remember:** Security is a process, not a product. Stay vigilant, but don't let perfect be the enemy of good. The measures above provide strong protection while remaining practical for a small team.
