// high-level Modern C++ HWID library designed by me, see https://github.com/Unchpokable/Identy
// add-to-this-project How to:
// > mkdir deps/
// > cd deps/
// > git clone https://github.com/Unchpokable/Identy.git
// > you're awesome
#include <Identy.h>

#include <algorithm>

#include "hwprotect_capi.hxx"

namespace
{
template<std::size_t BufferSize>
struct Ring
{
    std::uint8_t* external_data;

    Ring(std::uint8_t* data) : external_data(data)
    {
    }

    std::uint8_t& operator[](std::size_t index)
    {
        return external_data[index % BufferSize];
    }
};
} // namespace

namespace
{
constexpr std::uint64_t fnv1a_hash(std::string_view str) noexcept
{
    constexpr std::uint64_t prime = 0x100000001b3;
    std::uint64_t hash = static_cast<std::uint64_t>(0xcbf29ce484222325ULL);
    for(char c : str) {
        hash ^= static_cast<std::uint64_t>(static_cast<unsigned char>(c));
        hash = (hash * prime) & 0x7FFFFFFFFFFFFFFF;
    }
    return hash;
}

constexpr std::uint64_t operator""_hash(const char* str, std::size_t len) noexcept
{
    return fnv1a_hash(std::string_view(str, len));
}

constexpr auto salt = "<HWPROTECT_21012026_SALT>"_hash;

void salt_hash(HWID_SHA256& hwid_hash)
{
    auto salt_as_bytes = reinterpret_cast<const std::uint8_t*>(&salt);

    for(std::size_t chunk = 0; chunk < 4; ++chunk) {
        for(std::size_t i = 0; i < 8; ++i) {
            hwid_hash.data[chunk * 8 + i] ^= salt_as_bytes[i];
        }
    }
}
} // namespace

void hwprotect_encrypt_string(const char* source_str, char* buffer, int32_t length, bool snap_drives)
{
    auto hwid_hash = hwprotect_get_hwid(snap_drives);
    salt_hash(hwid_hash);

    Ring<32> hash_ring(hwid_hash.data);

    for(auto i { 0 }; i < std::min(std::strlen(source_str), static_cast<std::size_t>(length)); ++i) {
        buffer[i] = source_str[i] ^ hash_ring[i];
    }
}

void hwprotect_decrypt_string(const char* encrypted_str, int32_t encrypted_str_length, char* out_buffer, int32_t out_buffer_length,
    bool snap_drives)
{
    auto hwid_hash = hwprotect_get_hwid(snap_drives);
    salt_hash(hwid_hash);

    Ring<32> hash_ring(hwid_hash.data);

    for(auto i { 0 }; i < std::min(encrypted_str_length, out_buffer_length); ++i) {
        out_buffer[i] = encrypted_str[i] ^ hash_ring[i];
    }
}

HWID_SHA256 hwprotect_get_hwid(bool snap_drives)
{
    identy::hs::Hash256 hash;

    if(snap_drives) {
        auto snapshot = identy::snap_motherboard();
        hash = identy::hs::hash(snapshot);
    }
    else {
        auto snapshot = identy::snap_motherboard_ex();
        hash = identy::hs::hash(snapshot);
    }

    HWID_SHA256 result;

    std::memcpy(result.data, hash.buffer, sizeof(HWID_SHA256));

    return result;
}
