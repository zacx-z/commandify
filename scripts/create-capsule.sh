#!/bin/bash
if [ -z "$1" ]; then
    echo "[ERR]Usage: $0 <object-name>" >&2
    exit 1
fi

name="$1"

echo "create $name --with MeshFilter,MeshRenderer"
echo "set obj $~"
echo "property set \$obj:MeshFilter m_Mesh BuiltinResources/New-Capsule.fbx::Mesh"
echo "property set \$obj:MeshRenderer m_Materials [BuiltinExtra/Default-Diffuse.mat::Material]"
