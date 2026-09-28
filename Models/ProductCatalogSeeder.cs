using Awai.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Awai.Models
{
    /// <summary>
    /// Keeps the full published catalog available on Apply, while ensuring the three
    /// agency products exist. Does not delete products linked to applications.
    /// </summary>
    public static class ProductCatalogSeeder
    {
        private static readonly Guid[] SeedIds =
        [
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444441"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444442"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444443"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444444"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444445"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444446"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444447"),
            Guid.Parse("d4d4d4d4-4444-4444-8444-444444444448")
        ];

        private static readonly (string Title, string Short, string Icon, decimal Price)[] AgencyThree =
        [
            ("مسافرين", "تغطية سفر وحماية للمسافرين أثناء الرحلات.", "bi-airplane", 500),
            ("بطاقة عربية موحدة (برتقالية)", "البطاقة العربية الموحدة (البرتقالية) للتأمين على المركبات.", "bi-card-heading", 800),
            ("تأمين سيارة اجباري", "التأمين الإجباري على السيارات وفق المتطلبات المحلية.", "bi-car-front", 450)
        ];

        private static readonly (string Title, string Short, string Icon, decimal Price)[] Restored =
        [
            ("تأمين الحياة", "حماية مالية لأسرتك وضمان لمستقبلهم.", "bi-people", 1500),
            ("تأمين البضائع والنقل", "حماية بضائعك أثناء النقل المحلي والدولي.", "bi-truck", 2000),
            ("التأمين الهندسي", "حماية مشاريعك ومعداتك أثناء التنفيذ والتشغيل.", "bi-buildings", 3500),
            ("تأمين المسؤولية", "حماية من مطالبات الغير وتغطية المسؤوليات القانونية.", "bi-briefcase", 2200),
            ("تأمين الحوادث الشخصية", "تعويض مالي في حال وقوع حادث.", "bi-person-badge", 800)
        ];

        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;

            for (var i = 0; i < AgencyThree.Length; i++)
                await UpsertAsync(db, SeedIds[i], AgencyThree[i], i + 1, created, now);

            for (var i = 0; i < Restored.Length; i++)
                await UpsertAsync(db, SeedIds[i + 3], Restored[i], i + 4, created, now);

            // Publish every product so Apply shows the full catalog.
            var all = await db.Products.ToListAsync();
            foreach (var p in all)
            {
                if (!p.IsPublished)
                {
                    p.IsPublished = true;
                    p.Modified = now;
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task UpsertAsync(
            AppDbContext db,
            Guid id,
            (string Title, string Short, string Icon, decimal Price) spec,
            int sortOrder,
            DateTime created,
            DateTime now)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? await db.Products.FirstOrDefaultAsync(p => p.Title == spec.Title);

            if (product == null)
            {
                db.Products.Add(new Product
                {
                    Id = id,
                    Title = spec.Title,
                    ShortDescription = spec.Short,
                    Details = spec.Short,
                    IconClass = spec.Icon,
                    Price = spec.Price,
                    PriceNote = "يبدأ من",
                    SortOrder = sortOrder,
                    IsPublished = true,
                    Created = created
                });
                return;
            }

            product.Title = spec.Title;
            product.ShortDescription = spec.Short;
            product.Details = spec.Short;
            product.IconClass = spec.Icon;
            product.Price = spec.Price;
            product.PriceNote = "يبدأ من";
            product.SortOrder = sortOrder;
            product.IsPublished = true;
            product.Modified = now;
        }
    }
}
