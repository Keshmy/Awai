using Awai.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Awai.Models
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<SiteInfo>().HasData(new SiteInfo
            {
                Id = Guid.Parse("a1a1a1a1-1111-4111-8111-111111111111"),
                Name = "الواحة العربية للتأمين",
                Slogan = "ثقة تحمي المستقبل",
                Phone = "+218 94-0001097",
                Phone2 = "0940001096",
                WhatsAppNumber = "218940001097",
                Email = "info@awai.ly",
                Address = "شارع قرجي، طرابلس — أمام تجيش غوط الشعال سابقاً وسوق العرب ماركت (V36X+84V)",
                MapUrl = "https://maps.app.goo.gl/9JnJBwVbKfeF15BS9",
                MapEmbedUrl = "https://maps.google.com/maps?q=V36X%2B84V&hl=ar&z=17&output=embed",
                LogoUrl = "logo-mark.png",
                Created = created
            });

            modelBuilder.Entity<PageContent>().HasData(
                new PageContent
                {
                    Id = Guid.Parse("b2b2b2b2-2222-4222-8222-222222222221"),
                    Key = "AboutUs",
                    Title = "من نحن",
                    Body = "الواحة العربية للتأمين شركة ليبية تقدم حلولاً تأمينية متكاملة للأفراد والشركات بجودة واحتراف. نلتزم بحماية ما يهمك وتوفير الاطمئنان لك ولأسرتك.",
                    Created = created
                },
                new PageContent
                {
                    Id = Guid.Parse("b2b2b2b2-2222-4222-8222-222222222222"),
                    Key = "WhoWeAre",
                    Title = "من نكون",
                    Body = "فريق الواحة العربية يعمل على تقديم تغطيات صحية، مركبات، ممتلكات، حياة، نقل، هندسي، مسؤولية، وحوادث شخصية. شعارنا: ثقة تحمي المستقبل.",
                    Created = created
                });

            modelBuilder.Entity<Product>().HasData(
                Product(1, "التأمين الصحي", "رعاية صحية متميزة لك ولأسرتك.", "bi-heart-pulse", 2500),
                Product(2, "تأمين الممتلكات", "حماية ممتلكاتك من الحريق والسرقة والمخاطر المختلفة.", "bi-house-door", 1800),
                Product(3, "تأمين المركبات", "تغطية شاملة لمركبتك ضد الحوادث والسرقة والأضرار.", "bi-car-front", 1200),
                Product(4, "تأمين الحياة", "حماية مالية لأسرتك وضمان لمستقبلهم.", "bi-people", 1500),
                Product(5, "تأمين البضائع والنقل", "حماية بضائعك أثناء النقل المحلي والدولي.", "bi-truck", 2000),
                Product(6, "التأمين الهندسي", "حماية مشاريعك ومعداتك أثناء التنفيذ والتشغيل.", "bi-buildings", 3500),
                Product(7, "تأمين المسؤولية", "حماية من مطالبات الغير وتغطية المسؤوليات القانونية.", "bi-briefcase", 2200),
                Product(8, "تأمين الحوادث الشخصية", "تعويض مالي في حال وقوع حادث.", "bi-person-badge", 800)
            );

            modelBuilder.Entity<Post>().HasData(new Post
            {
                Id = Guid.Parse("c3c3c3c3-3333-4333-8333-333333333331"),
                Title = "حلول تأمينية متكاملة",
                Body = "نقدم باقات تناسب الأفراد والشركات. تواصل معنا أو قدّم طلبك عبر الموقع.",
                IsPublished = true,
                PublishedAt = created,
                Created = created
            });
        }

        private static Product Product(int order, string title, string shortDesc, string icon, decimal price)
        {
            var ids = new[]
            {
                "d4d4d4d4-4444-4444-8444-444444444441",
                "d4d4d4d4-4444-4444-8444-444444444442",
                "d4d4d4d4-4444-4444-8444-444444444443",
                "d4d4d4d4-4444-4444-8444-444444444444",
                "d4d4d4d4-4444-4444-8444-444444444445",
                "d4d4d4d4-4444-4444-8444-444444444446",
                "d4d4d4d4-4444-4444-8444-444444444447",
                "d4d4d4d4-4444-4444-8444-444444444448"
            };

            return new Product
            {
                Id = Guid.Parse(ids[order - 1]),
                Title = title,
                ShortDescription = shortDesc,
                Details = shortDesc,
                IconClass = icon,
                Price = price,
                PriceNote = "يبدأ من / سنوياً",
                SortOrder = order,
                IsPublished = true,
                Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }
    }
}
