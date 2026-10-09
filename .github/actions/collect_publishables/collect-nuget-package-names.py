"""Read NuGet package identities without inferring IDs from archive filenames."""

import json
import re
from pathlib import Path
from xml.etree import ElementTree
from zipfile import ZipFile


def collect_package_names(directory):
    names = {}
    for package in sorted(Path(directory).glob("*.nupkg")):
        with ZipFile(package) as archive:
            manifests = [name for name in archive.namelist() if name.lower().endswith(".nuspec")]
            if len(manifests) != 1:
                raise ValueError(f"{package.name}: expected exactly one nuspec")
            document = ElementTree.fromstring(archive.read(manifests[0]))
            identity = document.find("{*}metadata/{*}id")
            name = identity.text.strip() if identity is not None and identity.text else ""
            if not re.fullmatch(r"[A-Za-z0-9_.-]+", name):
                raise ValueError(f"{package.name}: missing or invalid package ID")
            names.setdefault(name.casefold(), name)
    return [names[key] for key in sorted(names)]


if __name__ == "__main__":
    print("package-names=" + json.dumps(collect_package_names(".artifacts/nuget"), separators=(",", ":")))
