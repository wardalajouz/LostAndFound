using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class CampusLocationConfiguration : IEntityTypeConfiguration<CampusLocation>
{
    public void Configure(EntityTypeBuilder<CampusLocation> builder)
    {
        builder.Property(l => l.NameEn).HasMaxLength(100);
        builder.Property(l => l.NameAr).HasMaxLength(100);

        // Starting list. Check names against the official campus map, and never change an
        // existing Id: add new rows instead, and set IsActive = false to retire one.
        builder.HasData(
            Location(1, "Faculty of Information Technology and Computer Sciences", "كلية تكنولوجيا المعلومات وعلوم الحاسوب"),
            Location(2, "Hijjawi Faculty for Engineering Technology", "كلية الحجاوي للهندسة التكنولوجية"),
            Location(3, "Faculty of Science", "كلية العلوم"),
            Location(4, "Faculty of Arts", "كلية الآداب"),
            Location(5, "Faculty of Economics and Administrative Sciences", "كلية الاقتصاد والعلوم الإدارية"),
            Location(6, "Faculty of Educational Sciences", "كلية العلوم التربوية"),
            Location(7, "Faculty of Sharia and Islamic Studies", "كلية الشريعة والدراسات الإسلامية"),
            Location(8, "Faculty of Law", "كلية القانون"),
            Location(9, "Faculty of Mass Communication", "كلية الإعلام"),
            Location(10, "Faculty of Fine Arts", "كلية الفنون الجميلة"),
            Location(11, "Faculty of Physical Education and Sport Sciences", "كلية التربية البدنية وعلوم الرياضة"),
            Location(12, "Faculty of Archaeology and Anthropology", "كلية الآثار والأنثروبولوجيا"),
            Location(13, "Faculty of Tourism and Hotel Management", "كلية السياحة والفنادق"),
            Location(14, "Faculty of Medicine", "كلية الطب"),
            Location(15, "Faculty of Pharmacy", "كلية الصيدلة"),
            Location(16, "Library", "المكتبة"),
            Location(17, "Main Gate", "البوابة الرئيسية"),
            Location(18, "Deanship of Student Affairs", "عمادة شؤون الطلبة"),
            Location(19, "Mosque", "المسجد"),
            Location(20, "Cafeterias", "الكافتيريات"),
            Location(21, "Sports Fields", "الملاعب الرياضية"),
            Location(22, "Parking Areas", "مواقف السيارات"),
            Location(23, "Bus Stop", "موقف الباصات"),
            Location(24, "Other", "مكان آخر"));
    }

    private static CampusLocation Location(int id, string nameEn, string nameAr) =>
        new() { Id = id, NameEn = nameEn, NameAr = nameAr, IsActive = true };
}
