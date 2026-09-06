using DevGitAtom.Contracts;
namespace DevGitAtom.Engine;

public class AtomRegistry
{
    private readonly Dictionary<string, IAtom> _atoms = [];

    public void Register(IAtom atom)
    {
        if (_atoms.ContainsKey(atom.Id))
            throw new InvalidOperationException($"Atom Id trung: '{atom.Id}' da duoc dang ky.");
        _atoms[atom.Id] = atom;
    }

    public IAtom Get(string id) =>
        _atoms.TryGetValue(id, out var atom) ? atom
        : throw new KeyNotFoundException($"Atom khong ton tai: '{id}'");

    public IReadOnlyList<IAtom> GetAll() => [.. _atoms.Values];

    public IReadOnlyList<IAtom> GetByCategory(AtomCategory category) =>
        [.. _atoms.Values.Where(a => a.Category == category)];
}

