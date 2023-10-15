using Austra.Parser;

namespace Austra;

public sealed class Session : Entity
{
    /// <summary>Creates a session from a persisted JSON file.</summary>
    /// <param name="fileName">A JSON file with series and definitions.</param>
    public Session(string fileName)
    {
        Engine = SessionEngine.Deserialize(fileName);
        DataSource = Engine.Source;
    }

    public IList<Definition> TroubledDefinitions => DataSource.TroubledDefinitions;

    public IDataSource DataSource { get; }

    public IAustraEngine Engine { get; }

    private sealed class SessionEngine : AustraEngine
    {
        private readonly string fromFile;

        public SessionEngine(IDataSource source, string fromFile) : base(source) =>
            this.fromFile = fromFile;

        public override void Define(Definition definition) =>
            ((IAustraEngine)this).Serialize(fromFile);

        public override void Undefine(IList<string> definitions) =>
            ((IAustraEngine)this).Serialize(fromFile);

        /// <summary>Deserializes a datasource from an UTF-8 file.</summary>
        /// <param name="fileName">An UTF-8 file previously serialized.</param>
        /// <returns>An object implementing this interface.</returns>
        public static new IAustraEngine Deserialize(string fileName)
        {
            SessionEngine newEngine = new(new DataSource(), fileName);
            newEngine.DeserializeSource(fileName);
            return newEngine;
        }
    }
}
