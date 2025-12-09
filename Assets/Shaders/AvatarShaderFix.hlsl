// Avatar Shader Fix Include
#ifndef AVATAR_SHADER_FIX_INCLUDED
#define AVATAR_SHADER_FIX_INCLUDED

// Missing submesh type definitions
#ifndef avatar_SUBMESH_TYPE_OUTFIT
#define avatar_SUBMESH_TYPE_OUTFIT 0
#define avatar_SUBMESH_TYPE_BODY 1
#define avatar_SUBMESH_TYPE_HAIR 2
#endif

// Missing function definition
#ifndef avatar_IsSubmeshType
bool avatar_IsSubmeshType(int submeshType, int targetType) { return submeshType == targetType; }
#endif

#endif
