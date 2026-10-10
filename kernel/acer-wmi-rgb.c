// SPDX-License-Identifier: GPL-2.0-or-later
/*
 * acer-wmi-rgb.c: Acer Nitro / Predator 4-zone keyboard RGB control driver
 *
 * Exposes the Acer "AcerGamingFunction" keyboard backlight WMI interface
 * that NitroSense uses on Windows.  Reverse engineered from the OEM
 * ACPI table (SSDT14, OEM ID "ACRSYS") of an Acer Nitro 5 AN515-58 and from
 * the NitroSense/Care Center binaries shipped for the same machine.
 *
 * The ACPI control method for the keyboard backlight ("WMBH") is bound to
 * WMI GUID 7A4DDFE7-5B5D-40B4-8595-4408E0CC7F56.  Its arguments are
 * (instance, method_id, input_buffer); we use method ids:
 *
 *   0x06  set zone static color
 *         input  = { zone, r, g, b, logo_flag, logo_index, 0, 0, 0, 0 }
 *                  zone: 1 = zone1, 2 = zone2, 4 = zone3, 8 = zone4
 *         output = { status, 0, 0, 0 }        (status == 0 on success)
 *
 *   0x07  get zone static color
 *         input  = { zone, 0, 0, ... }
 *         output = { status, r, g, b }
 *
 * Writes/reads land in EC RAM (KB1R/KB1G/KB1B .. KB4B) via the ACPI EC
 * region, so a set followed by a get reflects the firmware state.
 */

#include <linux/acpi.h>
#include <linux/errno.h>
#include <linux/init.h>
#include <linux/kernel.h>
#include <linux/module.h>
#include <linux/sysfs.h>
#include <linux/wmi.h>

#define WMI_GUID "7A4DDFE7-5B5D-40B4-8595-4408E0CC7F56"
MODULE_ALIAS("wmi:" WMI_GUID);

#ifdef pr_fmt
#undef pr_fmt
#endif
#define pr_fmt(fmt) "%s: " fmt, KBUILD_MODNAME

#define ACER_RGB_CMD_SET 0x06
#define ACER_RGB_CMD_GET 0x07

#define ACER_RGB_ZONES 4
static const u8 zone_sel[ACER_RGB_ZONES] = { 0x01, 0x02, 0x04, 0x08 };

struct acer_rgb_color {
	u8 r, g, b;
};

static struct acer_rgb_color zone_color[ACER_RGB_ZONES];
static bool zone_valid[ACER_RGB_ZONES];

/* 10-byte argument block, mirrors the OEM input buffer layout. */
struct acer_rgb_args {
	u8 zone;
	u8 r;
	u8 g;
	u8 b;
	u8 logo_flag;
	u8 logo_index;
	u8 reserved[4];
} __packed;

static acpi_status acer_rgb_wmi_call(u8 method, struct acer_rgb_args *in,
				     u8 *out, size_t out_size)
{
	struct acpi_buffer input = { sizeof(*in), in };
	struct acpi_buffer output = { ACPI_ALLOCATE_BUFFER, NULL };
	union acpi_object *obj;
	acpi_status status;

	status = wmi_evaluate_method(WMI_GUID, 0, method, &input, &output);
	if (ACPI_FAILURE(status))
		return status;

	obj = output.pointer;
	if (!obj)
		return AE_ERROR;
	if (obj->type != ACPI_TYPE_BUFFER) {
		kfree(obj);
		return AE_ERROR;
	}

	if (obj->buffer.length >= out_size)
		memcpy(out, obj->buffer.pointer, out_size);
	else
		memset(out, 0, out_size);

	kfree(obj);
	return AE_OK;
}

static int acer_rgb_set(int idx, u8 r, u8 g, u8 b)
{
	struct acer_rgb_args in = {
		.zone = zone_sel[idx],
		.r = r,
		.g = g,
		.b = b,
	};
	u8 out[4] = { 0xff, 0, 0, 0 };
	acpi_status status;

	status = acer_rgb_wmi_call(ACER_RGB_CMD_SET, &in, out, sizeof(out));
	if (ACPI_FAILURE(status))
		return -EIO;

	if (out[0] != 0)
		return -EINVAL;

	zone_color[idx].r = r;
	zone_color[idx].g = g;
	zone_color[idx].b = b;
	zone_valid[idx] = true;
	return 0;
}

static int acer_rgb_get(int idx, u8 *r, u8 *g, u8 *b)
{
	struct acer_rgb_args in = {
		.zone = zone_sel[idx],
	};
	u8 out[4] = { 0xff, 0, 0, 0 };
	acpi_status status;

	status = acer_rgb_wmi_call(ACER_RGB_CMD_GET, &in, out, sizeof(out));
	if (ACPI_FAILURE(status))
		return -EIO;
	if (out[0] != 0)
		return -EINVAL;

	*r = out[1];
	*g = out[2];
	*b = out[3];
	return 0;
}

static int acer_rgb_zone_store(int idx, const char *buf, size_t count)
{
	unsigned int r, g, b;
	int ret;

	/* Accept "RRGGBB" (hex), optionally "#RRGGBB". */
	if (buf[0] == '#')
		buf++;

	if (sscanf(buf, "%2x%2x%2x", &r, &g, &b) != 3)
		return -EINVAL;
	if (r > 0xff || g > 0xff || b > 0xff)
		return -EINVAL;

	ret = acer_rgb_set(idx, r, g, b);
	if (ret)
		return ret;

	return count;
}

static ssize_t acer_rgb_zone_show(int idx, char *buf)
{
	u8 r, g, b;
	int ret;

	ret = acer_rgb_get(idx, &r, &g, &b);
	if (ret)
		return ret;

	return sysfs_emit(buf, "%02x%02x%02x\n", r, g, b);
}

#define ZONE_ATTR(n)                                                            \
	static ssize_t zone##n##_show(struct device_driver *d, char *buf)       \
	{                                                                      \
		return acer_rgb_zone_show(n - 1, buf);                          \
	}                                                                      \
	static ssize_t zone##n##_store(struct device_driver *d, const char *buf,\
				       size_t count)                            \
	{                                                                      \
		return acer_rgb_zone_store(n - 1, buf, count);                  \
	}                                                                      \
	static DRIVER_ATTR_RW(zone##n)

ZONE_ATTR(1);
ZONE_ATTR(2);
ZONE_ATTR(3);
ZONE_ATTR(4);

static struct attribute *acer_rgb_attrs[] = {
	&driver_attr_zone1.attr,
	&driver_attr_zone2.attr,
	&driver_attr_zone3.attr,
	&driver_attr_zone4.attr,
	NULL
};
ATTRIBUTE_GROUPS(acer_rgb);

static const struct wmi_device_id acer_rgb_id_table[] = {
	{ .guid_string = WMI_GUID },
	{},
};
MODULE_DEVICE_TABLE(wmi, acer_rgb_id_table);

static struct wmi_driver acer_rgb_driver = {
	.driver = {
		.name = "acer-wmi-rgb",
		.groups = acer_rgb_groups,
	},
	.id_table = acer_rgb_id_table,
};

static int __init acer_rgb_init(void)
{
	if (!wmi_has_guid(WMI_GUID)) {
		pr_err("Acer keyboard RGB WMI guid not found\n");
		return -ENODEV;
	}
	return wmi_driver_register(&acer_rgb_driver);
}

static void __exit acer_rgb_exit(void)
{
	wmi_driver_unregister(&acer_rgb_driver);
}

module_init(acer_rgb_init);
module_exit(acer_rgb_exit);

MODULE_DESCRIPTION("Acer Nitro/Predator keyboard RGB (4-zone) WMI driver");
MODULE_AUTHOR("Reverse engineered from Acer Nitro 5 AN515-58 OEM tables");
MODULE_LICENSE("GPL");
