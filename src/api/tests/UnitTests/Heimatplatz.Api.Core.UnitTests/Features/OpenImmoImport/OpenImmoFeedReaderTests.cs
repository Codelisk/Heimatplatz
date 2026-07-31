using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.OpenImmoImport;

[TestFixture]
public class OpenImmoFeedReaderTests
{
    private string _tempDir = null!;
    private OpenImmoFeedReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"openimmo-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _reader = new OpenImmoFeedReader();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteFile(string name, string content, DateTime? lastWriteUtc = null)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        if (lastWriteUtc.HasValue)
            File.SetLastWriteTimeUtc(path, lastWriteUtc.Value);
        return path;
    }

    [Test]
    public void FindLatestFeedFile_LeererOrdner_LiefertNull()
    {
        _reader.FindLatestFeedFile(_tempDir, TimeSpan.Zero).Should().BeNull();
    }

    [Test]
    public void FindLatestFeedFile_FehlendesVerzeichnis_LiefertNull()
    {
        _reader.FindLatestFeedFile(Path.Combine(_tempDir, "gibtsnicht"), TimeSpan.Zero).Should().BeNull();
    }

    [Test]
    public void FindLatestFeedFile_NimmtNeuesteDateiUndIgnoriertTempDateien()
    {
        WriteFile("alt.xml", "<a/>", DateTime.UtcNow.AddHours(-3));
        WriteFile("neu.xml", "<a/>", DateTime.UtcNow.AddHours(-1));
        WriteFile(".hidden.xml", "<a/>", DateTime.UtcNow);
        WriteFile("upload.xml.tmp", "<a/>", DateTime.UtcNow);
        WriteFile("readme.txt", "kein feed", DateTime.UtcNow);

        var result = _reader.FindLatestFeedFile(_tempDir, TimeSpan.FromMinutes(2));

        result.Should().NotBeNull();
        result!.FileName.Should().Be("neu.xml");
        result.IsStable.Should().BeTrue();
    }

    [Test]
    public void FindLatestFeedFile_FrischeDatei_IstNichtStabil()
    {
        WriteFile("frisch.xml", "<a/>");

        var result = _reader.FindLatestFeedFile(_tempDir, TimeSpan.FromMinutes(2));

        result.Should().NotBeNull();
        result!.IsStable.Should().BeFalse("die Datei koennte noch hochgeladen werden");
    }

    [Test]
    public void OpenFeedFile_PlainXml_LiefertStreamOhneZip()
    {
        var path = WriteFile("feed.xml", "<openimmo/>");

        using var content = _reader.OpenFeedFile(path, maxArchiveUncompressedBytes: 1024);

        content.Zip.Should().BeNull();
        new StreamReader(content.XmlStream).ReadToEnd().Should().Be("<openimmo/>");
    }

    [Test]
    public void OpenFeedFile_Zip_LiefertGroesstenXmlEntryUndBildZugriff()
    {
        var zipPath = Path.Combine(_tempDir, "feed.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "meta.xml", "<meta/>");
            WriteEntry(archive, "objekte.xml", "<openimmo><anbieter/></openimmo>");
            WriteEntry(archive, "bilder/haus.jpg", "fake-jpeg-bytes");
        }

        using var content = _reader.OpenFeedFile(zipPath, maxArchiveUncompressedBytes: 1024 * 1024);

        new StreamReader(content.XmlStream).ReadToEnd().Should().Contain("anbieter");
        content.Zip.Should().NotBeNull();
        // Exakter Pfad und blanker Dateiname muessen beide matchen (OpenImmo-pfad
        // referenziert mal mit, mal ohne Ordner)
        content.Zip!.ReadEntry("bilder/haus.jpg", 1024).Should().NotBeNull();
        content.Zip.ReadEntry("haus.jpg", 1024).Should().NotBeNull();
        content.Zip.ReadEntry("fehlt.jpg", 1024).Should().BeNull();
        content.Zip.ReadEntry("bilder/haus.jpg", maxBytes: 3).Should().BeNull("Entry ueberschreitet das Limit");
    }

    [Test]
    public void OpenFeedFile_ZipOhneXml_Wirft()
    {
        var zipPath = Path.Combine(_tempDir, "leer.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            WriteEntry(archive, "bild.jpg", "x");

        var act = () => _reader.OpenFeedFile(zipPath, maxArchiveUncompressedBytes: 1024);

        act.Should().Throw<InvalidDataException>().WithMessage("*keinen XML-Entry*");
    }

    [Test]
    public void OpenFeedFile_ZipBombGuard_Wirft()
    {
        var zipPath = Path.Combine(_tempDir, "bomb.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            WriteEntry(archive, "objekte.xml", new string('x', 4096));

        var act = () => _reader.OpenFeedFile(zipPath, maxArchiveUncompressedBytes: 100);

        act.Should().Throw<InvalidDataException>().WithMessage("*Limit*");
    }

    [Test]
    public void OpenFeedFile_KaputtesZip_Wirft()
    {
        var path = WriteFile("kaputt.zip", "das ist kein zip");

        var act = () => _reader.OpenFeedFile(path, maxArchiveUncompressedBytes: 1024);

        act.Should().Throw<InvalidDataException>();
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
    }
}
