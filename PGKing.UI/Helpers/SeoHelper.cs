using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Infrastructure.Data;

namespace PGKing.UI.Helpers
{
    public static class SeoHelper
    {
        public static string GenerateSlug(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var slug = input.ToLowerInvariant().Trim();
            var sb = new StringBuilder();
            bool prevDash = false;
            foreach (char c in slug)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevDash = false;
                }
                else
                {
                    if (!prevDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
            }
            return sb.ToString().TrimEnd('-');
        }

        public static string GenerateLocationSlug(string? area, string? city)
        {
            var areaSlug = GenerateSlug(area);
            var citySlug = GenerateSlug(city);
            if (string.IsNullOrEmpty(citySlug)) citySlug = "mumbai";
            if (string.IsNullOrEmpty(areaSlug)) return $"pg-in-{citySlug}";
            return $"pg-in-{areaSlug}-{citySlug}";
        }

        public static async Task<string> GenerateUniquePropertySlugAsync(
            ApplicationDbContext context,
            string? title,
            string locationSlug,
            int? currentPropertyId = null)
        {
            var baseSlug = GenerateSlug(title);
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "pg";

            var slug = baseSlug;
            int counter = 2;

            while (await context.Properties.AnyAsync(p =>
                p.LocationSlug == locationSlug &&
                p.PropertySlug == slug &&
                (!currentPropertyId.HasValue || p.Id != currentPropertyId.Value)))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        public static string GenerateCanonicalUrl(string locationSlug, string propertySlug)
        {
            return $"https://pgking.in/{locationSlug}/{propertySlug}";
        }

        public static async Task UpdateXmlSitemapAsync(ApplicationDbContext context, string webRootPath)
        {
            try
            {
                var properties = await context.Properties
                    .Include(p => p.City)
                    .Include(p => p.State)
                    .AsNoTracking()
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine("<?xml-stylesheet type=\"text/css\" href=\"https://www.xml-sitemaps.com/css/sitemap.css\"?>");
                sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

                // Static core pages
                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>daily</changefreq>");
                sb.AppendLine("       <priority>1.0000</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/paying-guests</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>daily</changefreq>");
                sb.AppendLine("       <priority>0.8000</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/Home/Gallery</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>weekly</changefreq>");
                sb.AppendLine("       <priority>0.8000</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/Home/Services</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>weekly</changefreq>");
                sb.AppendLine("       <priority>0.8000</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/Home/Team</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>monthly</changefreq>");
                sb.AppendLine("       <priority>0.6400</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/Home/AboutUs</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>monthly</changefreq>");
                sb.AppendLine("       <priority>0.6400</priority>");
                sb.AppendLine("  </url>");

                sb.AppendLine("  <url>");
                sb.AppendLine("       <loc>https://pgking.in/Home/Contact</loc>");
                sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                sb.AppendLine("       <changefreq>monthly</changefreq>");
                sb.AppendLine("       <priority>0.6400</priority>");
                sb.AppendLine("  </url>");

                // Location listing pages (unique area/city combinations)
                var locationSlugs = properties
                    .Select(p => !string.IsNullOrEmpty(p.LocationSlug) ? p.LocationSlug : GenerateLocationSlug(p.Area, p.CityName ?? p.City?.Name))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

                foreach (var loc in locationSlugs)
                {
                    sb.AppendLine("  <url>");
                    sb.AppendLine($"       <loc>https://pgking.in/{loc}</loc>");
                    sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                    sb.AppendLine("       <changefreq>daily</changefreq>");
                    sb.AppendLine("       <priority>0.9000</priority>");
                    sb.AppendLine("  </url>");
                }

                // Individual property canonical SEO URLs (NO old URLs)
                foreach (var property in properties)
                {
                    var locSlug = !string.IsNullOrEmpty(property.LocationSlug) ? property.LocationSlug : GenerateLocationSlug(property.Area, property.CityName ?? property.City?.Name);
                    var propSlug = !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : GenerateSlug(property.Title);
                    if (propSlug.StartsWith("pg-")) propSlug = propSlug.Substring(3);
                    var canonicalUrl = property.CanonicalUrl ?? GenerateCanonicalUrl(locSlug, propSlug);

                    sb.AppendLine("  <url>");
                    sb.AppendLine($"       <loc>{canonicalUrl}</loc>");
                    sb.AppendLine($"       <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss+00:00}</lastmod>");
                    sb.AppendLine("       <changefreq>daily</changefreq>");
                    sb.AppendLine("       <priority>0.8000</priority>");
                    sb.AppendLine("  </url>");
                }

                sb.AppendLine("</urlset>");

                var sitemapPath = Path.Combine(webRootPath, "sitemap.xml");
                await File.WriteAllTextAsync(sitemapPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Suppress sitemap writing errors in non-web environments
            }
        }
    }
}
