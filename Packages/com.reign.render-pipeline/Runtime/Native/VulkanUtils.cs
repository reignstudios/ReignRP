using System;
using System.Runtime.InteropServices;

using uint32_t = System.UInt32;
using VkFlags = System.UInt32;
using VkSurfaceTransformFlagsKHR = System.UInt32;
using VkCompositeAlphaFlagsKHR = System.UInt32;
using VkImageUsageFlags = System.UInt32;
using VkPhysicalDevice = System.IntPtr;
using VkSurfaceKHR = System.IntPtr;

namespace Reign.SRP
{
    [StructLayout(LayoutKind.Sequential)]
    struct VkExtent2D
    {
        uint32_t    width;
        uint32_t    height;
    }

    [Flags]
    enum VkSurfaceTransformFlagBitsKHR
    {
        VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR = 0x00000001,
        VK_SURFACE_TRANSFORM_ROTATE_90_BIT_KHR = 0x00000002,
        VK_SURFACE_TRANSFORM_ROTATE_180_BIT_KHR = 0x00000004,
        VK_SURFACE_TRANSFORM_ROTATE_270_BIT_KHR = 0x00000008,
        VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_BIT_KHR = 0x00000010,
        VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_ROTATE_90_BIT_KHR = 0x00000020,
        VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_ROTATE_180_BIT_KHR = 0x00000040,
        VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_ROTATE_270_BIT_KHR = 0x00000080,
        VK_SURFACE_TRANSFORM_INHERIT_BIT_KHR = 0x00000100,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct VkSurfaceCapabilitiesKHR
    {
        uint32_t                         minImageCount;
        uint32_t                         maxImageCount;
        VkExtent2D                       currentExtent;
        VkExtent2D                       minImageExtent;
        VkExtent2D                       maxImageExtent;
        uint32_t                         maxImageArrayLayers;
        VkSurfaceTransformFlagsKHR       supportedTransforms;
        VkSurfaceTransformFlagBitsKHR    currentTransform;
        VkCompositeAlphaFlagsKHR         supportedCompositeAlpha;
        VkImageUsageFlags                supportedUsageFlags;
    }

    enum VkResult
    {
        VK_SUCCESS = 0,
        VK_NOT_READY = 1,
        VK_TIMEOUT = 2,
        VK_EVENT_SET = 3,
        VK_EVENT_RESET = 4,
        VK_INCOMPLETE = 5,
        VK_ERROR_OUT_OF_HOST_MEMORY = -1,
        VK_ERROR_OUT_OF_DEVICE_MEMORY = -2,
        VK_ERROR_INITIALIZATION_FAILED = -3,
        VK_ERROR_DEVICE_LOST = -4,
        VK_ERROR_MEMORY_MAP_FAILED = -5,
        VK_ERROR_LAYER_NOT_PRESENT = -6,
        VK_ERROR_EXTENSION_NOT_PRESENT = -7,
        VK_ERROR_FEATURE_NOT_PRESENT = -8,
        VK_ERROR_INCOMPATIBLE_DRIVER = -9,
        VK_ERROR_TOO_MANY_OBJECTS = -10,
        VK_ERROR_FORMAT_NOT_SUPPORTED = -11,
        VK_ERROR_FRAGMENTED_POOL = -12,
        VK_ERROR_UNKNOWN = -13,
        // Provided by VK_VERSION_1_1
        VK_ERROR_OUT_OF_POOL_MEMORY = -1000069000,
        // Provided by VK_VERSION_1_1
        VK_ERROR_INVALID_EXTERNAL_HANDLE = -1000072003,
        // Provided by VK_VERSION_1_2
        VK_ERROR_FRAGMENTATION = -1000161000,
        // Provided by VK_VERSION_1_2
        VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS = -1000257000,
        // Provided by VK_VERSION_1_3
        VK_PIPELINE_COMPILE_REQUIRED = 1000297000,
        // Provided by VK_KHR_surface
        VK_ERROR_SURFACE_LOST_KHR = -1000000000,
        // Provided by VK_KHR_surface
        VK_ERROR_NATIVE_WINDOW_IN_USE_KHR = -1000000001,
        // Provided by VK_KHR_swapchain
        VK_SUBOPTIMAL_KHR = 1000001003,
        // Provided by VK_KHR_swapchain
        VK_ERROR_OUT_OF_DATE_KHR = -1000001004,
        // Provided by VK_KHR_display_swapchain
        VK_ERROR_INCOMPATIBLE_DISPLAY_KHR = -1000003001,
        // Provided by VK_EXT_debug_report
        VK_ERROR_VALIDATION_FAILED_EXT = -1000011001,
        // Provided by VK_NV_glsl_shader
        VK_ERROR_INVALID_SHADER_NV = -1000012000,
        // Provided by VK_KHR_video_queue
        VK_ERROR_IMAGE_USAGE_NOT_SUPPORTED_KHR = -1000023000,
        // Provided by VK_KHR_video_queue
        VK_ERROR_VIDEO_PICTURE_LAYOUT_NOT_SUPPORTED_KHR = -1000023001,
        // Provided by VK_KHR_video_queue
        VK_ERROR_VIDEO_PROFILE_OPERATION_NOT_SUPPORTED_KHR = -1000023002,
        // Provided by VK_KHR_video_queue
        VK_ERROR_VIDEO_PROFILE_FORMAT_NOT_SUPPORTED_KHR = -1000023003,
        // Provided by VK_KHR_video_queue
        VK_ERROR_VIDEO_PROFILE_CODEC_NOT_SUPPORTED_KHR = -1000023004,
        // Provided by VK_KHR_video_queue
        VK_ERROR_VIDEO_STD_VERSION_NOT_SUPPORTED_KHR = -1000023005,
        // Provided by VK_EXT_image_drm_format_modifier
        VK_ERROR_INVALID_DRM_FORMAT_MODIFIER_PLANE_LAYOUT_EXT = -1000158000,
        // Provided by VK_KHR_global_priority
        VK_ERROR_NOT_PERMITTED_KHR = -1000174001,
        // Provided by VK_EXT_full_screen_exclusive
        VK_ERROR_FULL_SCREEN_EXCLUSIVE_MODE_LOST_EXT = -1000255000,
        // Provided by VK_KHR_deferred_host_operations
        VK_THREAD_IDLE_KHR = 1000268000,
        // Provided by VK_KHR_deferred_host_operations
        VK_THREAD_DONE_KHR = 1000268001,
        // Provided by VK_KHR_deferred_host_operations
        VK_OPERATION_DEFERRED_KHR = 1000268002,
        // Provided by VK_KHR_deferred_host_operations
        VK_OPERATION_NOT_DEFERRED_KHR = 1000268003,
        #if VK_ENABLE_BETA_EXTENSIONS
        // Provided by VK_KHR_video_encode_queue
        VK_ERROR_INVALID_VIDEO_STD_PARAMETERS_KHR = -1000299000,
        #endif
        // Provided by VK_EXT_image_compression_control
        VK_ERROR_COMPRESSION_EXHAUSTED_EXT = -1000338000,
        // Provided by VK_EXT_shader_object
        VK_ERROR_INCOMPATIBLE_SHADER_BINARY_EXT = 1000482000,
        // Provided by VK_KHR_maintenance1
        VK_ERROR_OUT_OF_POOL_MEMORY_KHR = VK_ERROR_OUT_OF_POOL_MEMORY,
        // Provided by VK_KHR_external_memory
        VK_ERROR_INVALID_EXTERNAL_HANDLE_KHR = VK_ERROR_INVALID_EXTERNAL_HANDLE,
        // Provided by VK_EXT_descriptor_indexing
        VK_ERROR_FRAGMENTATION_EXT = VK_ERROR_FRAGMENTATION,
        // Provided by VK_EXT_global_priority
        VK_ERROR_NOT_PERMITTED_EXT = VK_ERROR_NOT_PERMITTED_KHR,
        // Provided by VK_EXT_buffer_device_address
        VK_ERROR_INVALID_DEVICE_ADDRESS_EXT = VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS,
        // Provided by VK_KHR_buffer_device_address
        VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS_KHR = VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS,
        // Provided by VK_EXT_pipeline_creation_cache_control
        VK_PIPELINE_COMPILE_REQUIRED_EXT = VK_PIPELINE_COMPILE_REQUIRED,
        // Provided by VK_EXT_pipeline_creation_cache_control
        VK_ERROR_PIPELINE_COMPILE_REQUIRED_EXT = VK_PIPELINE_COMPILE_REQUIRED,
    }

    static class VulkanUtils
    {
        [DllImport("libGLESv3")]
        public static extern VkResult vkGetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, ref VkSurfaceCapabilitiesKHR pSurfaceCapabilities);
    }
}
