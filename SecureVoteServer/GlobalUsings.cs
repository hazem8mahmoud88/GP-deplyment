// ASP.NET Core
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.IdentityModel.Tokens;
global using FluentValidation.AspNetCore;


// Entity Framework Core
global using Microsoft.EntityFrameworkCore;

// FluentValidation
global using FluentValidation;

// SecureVote Namespaces
global using SecureVote.Abstractions;
global using SecureVote.Authentication;
global using SecureVote.Constants;
global using SecureVote.Encryption;
global using SecureVote.Entities;
global using SecureVote.Errors;
global using SecureVote.Extensions;
global using SecureVote.Persistence;
global using SecureVote.Services;

// Contracts
global using SecureVote.Contracts.Auth;
global using SecureVote.Contracts.Candidates;
global using SecureVote.Contracts.Elections;
global using SecureVote.Contracts.ElectionOrganizers;
global using SecureVote.Contracts.Organizer;

// System
global using System.Globalization;
global using System.IO.Compression;
global using System.Text;
