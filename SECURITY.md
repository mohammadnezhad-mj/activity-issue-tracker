# Security Guide - Activity Management System

This document describes the security measures implemented in the system.

## ✅ Implemented Security Measures

### 1. **HTTPS Enforcement**
- All communication in the Production environment uses HTTPS
- HSTS (HTTP Strict Transport Security) is enabled
- Automatic redirect from HTTP to HTTPS

### 2. **Security Headers**
- `X-Frame-Options: DENY` - prevents Clickjacking
- `X-Content-Type-Options: nosniff` - prevents MIME sniffing
- `X-XSS-Protection: 1; mode=block` - protects against XSS
- `Content-Security-Policy` - restricts the resources allowed to execute
- `Strict-Transport-Security` - enforces HTTPS
- `Referrer-Policy` - controls information sent in the Referrer

### 3. **Session Security**
- Session cookies with the `HttpOnly` flag (prevents JavaScript access)
- Session cookies with the `Secure` flag (HTTPS only)
- Session cookies with `SameSite=Strict` (protects against CSRF)
- Automatic session timeout after 8 hours of inactivity
- Custom name for the session cookie

### 4. **Rate Limiting**
- Limit of 5 login requests per 15 minutes per IP
- Prevents brute force attacks
- Appropriate message shown to the user when the limit is reached

### 5. **Input Validation & Sanitization**
- Input length checks
- HTML encoding to prevent XSS
- Server-side validation for all inputs
- Whitelist validation for dropdowns

### 6. **Password Security**
- Passwords hashed with SHA256 and a unique salt
- Salt derived from the user's first and last name
- Backward compatibility with legacy passwords
- Passwords are never stored as plain text

### 7. **CSRF Protection**
- `ValidateAntiForgeryToken` used on all POST forms
- Automatic token validation by ASP.NET Core

### 8. **Authorization & Access Control**
- Access checks in all controllers
- Session-based authentication
- Role-based access control (Admin, Regular User)
- Permission-based access control for different sections

### 9. **SQL Injection Protection**
- Uses Entity Framework Core (parameterized queries)
- No raw SQL queries
- Type-safe queries

### 10. **Error Handling**
- Error details are not shown to the user in Production
- Errors are logged for administrator review
- Generic, user-friendly error messages

### 11. **Audit Logging**
- All CRUD operations are logged
- Failed login attempts are logged
- User information, timestamp, and operation type are stored
- Changes can be traced

### 12. **XSS Protection**
- HTML encoding on all view output
- Uses Razor syntax, which encodes automatically
- Input sanitization before storage

## 🔒 Security Recommendations for Deployment

### 1. **SSL/TLS Certificate**
- Use a valid SSL certificate
- Configure it correctly in IIS or the web server

### 2. **Firewall**
- Restrict access to unnecessary ports
- Whitelist IP addresses if needed

### 3. **Database Security**
- Protect the SQLite file
- Perform regular backups
- Restrict access to the database file

### 4. **Configuration Security**
- Protect the `config.json` file
- Do not place it in a public directory
- Use environment variables for sensitive information

### 5. **Monitoring**
- Review logs regularly
- Monitor failed login attempts
- Alert on suspicious activity

### 6. **Updates**
- Update the .NET Runtime regularly
- Update NuGet packages
- Review security advisories

### 7. **Backup**
- Perform regular database backups
- Back up the `config.json` file
- Test restore procedures periodically

## ⚠️ Important Notes

1. **Admin password**: use a strong password
2. **Session timeout**: you can reduce the timeout if needed
3. **Rate limiting**: you can adjust the limit if needed
4. **Log retention**: clear logs periodically to avoid filling up the database

## 📝 Pre-Deployment Checklist

- [ ] SSL certificate installed
- [ ] HTTPS enabled
- [ ] Security headers verified
- [ ] Rate limiting enabled
- [ ] Session security configured
- [ ] Database backup performed
- [ ] config.json protected
- [ ] Firewall configured
- [ ] Monitoring enabled
- [ ] Error logging verified
