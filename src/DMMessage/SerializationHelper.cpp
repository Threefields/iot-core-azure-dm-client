﻿#include "stdafx.h"
#include "SerializationHelper.h"
#include "Blob.h"
#include "CurrentVersion.h"

using namespace Microsoft::Devices::Management::Message;
using namespace Platform;

Blob^ SerializationHelper::CreateEmptyBlob(uint32_t tag)
{
    return CreateBlobFromPtrSize(tag, nullptr, 0);
}

Blob^ SerializationHelper::CreateBlobFromPtrSize(uint32_t tag, const byte* byteptr, size_t size)
{
    size_t byteCount = PrefixSize + size;

    auto byteArray = ref new Array<byte>(byteCount);

    // First, put out the version (32 bits)
    uint32_t version = CurrentVersion;
    memcpy_s(byteArray->Data, byteCount, &version, sizeof(version));

    // Second, put out the 32-bit tag
    memcpy_s(byteArray->Data + sizeof(version), byteCount, &tag, sizeof(tag));

    // Followed by the serialized object:
    memcpy_s(byteArray->Data + PrefixSize, byteCount, byteptr, size);

    return Blob::CreateFromByteArray(byteArray);
}

Blob^ SerializationHelper::CreateBlobFromJson(uint32_t tag, JsonObject^ jsonObject)
{
    String^ str = jsonObject->Stringify();
    return CreateBlobFromPtrSize(tag, (const byte*)str->Data(), str->Length() * sizeof(wchar_t));
}

Blob^ SerializationHelper::CreateBlobFromByteArray(uint32_t tag, const Array<byte>^ bytes)
{
    const byte* byteptr = bytes->Data;
    return CreateBlobFromPtrSize(tag, byteptr, bytes->Length);
}

String^ SerializationHelper::GetStringFromBlob(const Blob^ blob)
{
    return ref new String(reinterpret_cast<wchar_t*>(blob->bytes->Data + PrefixSize), (blob->bytes->Length - PrefixSize) / sizeof(wchar_t));
}

void SerializationHelper::ReadDataFromBlob(const Blob^ blob, byte* buffer, size_t size)
{
    memcpy_s(buffer, size, blob->bytes->Data + PrefixSize, size);
}