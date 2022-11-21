namespace EcCodeGen;

public class Master
{
	public int GroupNum { get; set; }
	public int RpmsgBufferSize { get; set; } = 4096;
	public bool Debug { get; set; } = false;
	public List<Slave> Slaves { get; set; } = new List<Slave>();
	public List<Slave> Servos => Slaves.Where(x => x.Type == SlaveType.AcServo).ToList();
	public List<Slave> IOs => Slaves.Where(x => x.Type == SlaveType.DigitalIO).ToList();
	private List<string> VariableNamesToExport { get; } = new List<string>();
	public string PdoEntryRegs
	{
		get
		{
			var lookup = Slaves.ToLookup(x => x.GroupIndex);
			var ret = lookup.Select((x, idx) =>
				{
					var variable = (string grp) => $"unsigned int {grp}_head;\n";
					var header = (string grp) =>
$@"
const ec_pdo_entry_reg_t {grp}[] = {{
";
					var group_rx = $"pdo_rx_group_{idx}";
					var group_tx = $"pdo_tx_group_{idx}";
					var regs_rx_str = x.EcPdoEntryRegs(rx: true, group_rx).Aggregate((a, b) => a + b);
					var regs_tx_str = x.EcPdoEntryRegs(rx: false, group_tx).Aggregate((a, b) => a + b);

					this.VariableNamesToExport.Add($"unsigned int {group_rx}_head");
					this.VariableNamesToExport.Add($"unsigned int {group_tx}_head");
					this.VariableNamesToExport.Add($"ec_pdo_entry_reg_t {group_rx}[]");
					this.VariableNamesToExport.Add($"ec_pdo_entry_reg_t {group_tx}[]");

					return variable(group_rx) + variable(group_tx) + header(group_rx) + regs_rx_str + "};\n" + header(group_tx) + regs_tx_str + "};";

				}).Aggregate((a, b) => a + b);
			return "static unsigned int ignored;\n" + ret;
		}
	}
	public string SyncConfig
	{
		get
		{
			var lookup = Slaves.ToLookup(x => new { x.GroupIndex, x.RxPdoContent, x.TxPdoContent });

			var configs = lookup.Select((element, idx) =>
				{
					var group = $"group_{idx}";
					var slave = element.First();
					foreach (var s in element)
						s.SyncGroupName = $"group_{idx}_sync_info";


					var str = slave.SyncConfig(group, out var variables);
					this.VariableNamesToExport.AddRange(variables);
					return str;
				});
			return configs.Aggregate((a, b) => a + "\n" + b);
		}
	}


	public string PdoA2rServoGrpH
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"typedef struct pdo_a2r_servo pdo_a2r_servo_grp[{Servos.Count}];\n";
			}
			return lookup.Select((x, idx) => $"typedef struct pdo_a2r_servo_{idx} pdo_a2r_servo_grp{idx}[{x.Count()}];\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aServoGrpH
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"typedef struct pdo_r2a_servo pdo_r2a_servo_grp[{Servos.Count}];\n";
			}
			return lookup.Select((x, idx) => $"typedef struct pdo_r2a_servo_{idx} pdo_r2a_servo_grp{idx}[{x.Count()}];\n").Aggregate((a, b) => a + b);
		}
	}

	public string PdoA2rServoGrpHpp
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"using pdo_a2r_servo_grp = std::array<pdo_a2r_servo, {Servos.Count}>;\n";

			}
			return lookup.Select((x, idx) => $"using pdo_a2r_servo_grp{idx} = std::array<pdo_a2r_servo_{idx}, {x.Count()}>;\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aServoGrpHpp
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"using pdo_r2a_servo_grp = std::array<pdo_r2a_servo, {Servos.Count}>;\n";
			}
			return lookup.Select((x, idx) => $"using pdo_r2a_servo_grp{idx} = std::array<pdo_r2a_servo_{idx}, {x.Count()}>;\n").Aggregate((a, b) => a + b);
		}
	}

	public string PdoA2rIoGrpH
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"typedef struct pdo_a2r_io pdo_a2r_io_grp[{IOs.Count}];\n";
			}
			return lookup.Select((x, idx) => $"typedef struct pdo_a2r_io_{idx} pdo_a2r_io_grp{idx}[{x.Count()}];\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aIoGrpH
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"typedef struct pdo_r2a_io pdo_r2a_io_grp[{IOs.Count}];\n";
			}
			return lookup.Select((x, idx) => $"typedef struct pdo_r2a_io_{idx} pdo_r2a_io_grp{idx}[{x.Count()}];\n").Aggregate((a, b) => a + b);
		}
	}

	public string PdoA2rIoGrpHpp
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"using pdo_a2r_io_grp = std::array<pdo_a2r_io, {IOs.Count}>;\n";

			}
			return lookup.Select((x, idx) => $"using pdo_a2r_io_grp{idx} = std::array<pdo_a2r_io_{idx}, {x.Count()}>;\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aIoGrpHpp
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"using pdo_r2a_io_grp = std::array<pdo_r2a_io, {IOs.Count}>;\n";
			}
			return lookup.Select((x, idx) => $"using pdo_r2a_io_grp{idx} = std::array<pdo_r2a_io_{idx}, {x.Count()}>;\n").Aggregate((a, b) => a + b);
		}
	}

	public string PdoA2rServoStructs
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"struct pdo_a2r_servo {{\n{lookup.Single().Key}}};\n";
			}
			return lookup.Select((x, idx) => $"struct pdo_a2r_servo_{idx} {{\n{x.Key}}};\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aServoStructs
	{
		get
		{
			var lookup = Servos.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"struct pdo_r2a_servo {{\n{lookup.Single().Key}}};\n";
			}
			return lookup.Select((x, idx) => $"struct pdo_r2a_servo_{idx} {{\n{x.Key}}};\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoA2rIoStructs
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.RxPdoContent);
			if (lookup.Count == 1)
			{
				return $"struct pdo_a2r_io {{\n{lookup.Single().Key}}};\n";
			}
			return lookup.Select((x, idx) => $"struct pdo_a2r_io_{idx} {{\n{x.Key}}};\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoR2aIoStructs
	{
		get
		{
			var lookup = IOs.ToLookup(x => x.TxPdoContent);
			if (lookup.Count == 1)
			{
				return $"struct pdo_r2a_io {{\n{lookup.Single().Key}}};\n";
			}
			return lookup.Select((x, idx) => $"struct pdo_r2a_io_{idx} {{\n{x.Key}}};\n").Aggregate((a, b) => a + b);
		}
	}
	public string PdoA2r
	{
		get
		{
			var servo_lookup = Servos.ToLookup(x => x.RxPdoContent);
			var io_lookup = IOs.ToLookup(x => x.RxPdoContent);

			var servo_str = servo_lookup.Count > 1 ?
			servo_lookup.Select((x, idx) => $"\tpdo_a2r_servo_grp{idx} servo_grp_{idx};\n").Aggregate((a, b) => a + b) :
			 "\tpdo_a2r_servo_grp servo_grp;\n";
			var io_str = io_lookup.Count > 1 ? io_lookup.Select((x, idx) => $"\tpdo_a2r_io_grp{idx} io_grp_{idx};\n").Aggregate((a, b) => a + b) :
			 "\tpdo_a2r_io_grp io_grp;\n";

			return $"struct pdo_a2r {{\n{servo_str}{io_str}}};\n";
		}

	}
	public string PdoR2a
	{
		get
		{
			var servo_lookup = Servos.ToLookup(x => x.TxPdoContent);
			var io_lookup = IOs.ToLookup(x => x.TxPdoContent);
			var servo_str = servo_lookup.Count > 1 ?
			servo_lookup.Select((x, idx) => $"\tpdo_r2a_servo_grp{idx} servo_grp_{idx};\n").Aggregate((a, b) => a + b) :
			 "\tpdo_r2a_servo_grp servo_grp;\n";
			var io_str = io_lookup.Count > 1 ? io_lookup.Select((x, idx) => $"\tpdo_r2a_io_grp{idx} io_grp_{idx};\n").Aggregate((a, b) => a + b) :
			 "\tpdo_r2a_io_grp io_grp;\n";
			return $"struct pdo_r2a {{\n{servo_str}{io_str}}};\n";
		}

	}
	public string A2r
	{
		get
		{
			const int rpmsg_hdr_size = 17;
			int pdo_size = Slaves.Sum(x => x.RxPdoSize / 8);
			int pkg_size = (RpmsgBufferSize - rpmsg_hdr_size) / (pdo_size) - 1;
			string fmt = $@"
#define PKG_SIZE {pkg_size}
struct a2r {{
    uint16_t       domain_idx;
    uint16_t       pkg_num;
    uint16_t 	   control_word;
    struct pdo_a2r data[PKG_SIZE];
}} __attribute__((aligned(0x20)));
";
			return fmt;

		}

	}
	public static string R2a
	{
		get
		{
			const string fmt = @"
struct r2a {
    uint16_t       buffer_len;
    uint16_t       domain_idx;
    uint32_t       cycle_count;
    uint16_t 	   status_word;
    struct pdo_r2a data;
} __attribute__((aligned(0x20)));

			";
			return fmt;
		}
	}

	public string MsgH
	{
		get
		{
			string generated_comment = $"// Generated by EcGen at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
			const string defines = @"
#define CMD_ENTER_STANDBY 0x01
#define CMD_ENTER_CYCLE   0x02

#define STATUS_INIT          0x00
#define STATUS_ENTER_STANDBY 0x01
#define STATUS_EXIT_STANDBY  0x02
#define STATUS_STANDBY       0x03
#define STATUS_CYCLE_RUN     0x04

			";

			const string snippet_head = @"
#pragma once
#include <stdint.h>

#pragma pack(push, 1)

";

			const string snippet_end = @"

typedef struct pdo_a2r       pdo_a2r_t;
typedef struct pdo_r2a       pdo_r2a_t;
typedef struct a2r           a2r_t;
typedef struct r2a           r2a_t;

#pragma pack(pop)
";
			var a2r = PdoA2rServoStructs + '\n' + PdoA2rIoStructs + '\n' + PdoA2rServoGrpH + '\n' + PdoA2rIoGrpH + '\n' + PdoA2r + '\n' + A2r;
			var r2a = PdoR2aServoStructs + '\n' + PdoR2aIoStructs + '\n' + PdoR2aServoGrpH + '\n' + PdoR2aIoGrpH + '\n' + PdoR2a + '\n' + R2a;

			return generated_comment + defines + snippet_head + a2r + "\n\n\n" + r2a + snippet_end;

		}
	}
	public string MsgHpp
	{
		get
		{
			string generated_comment = $"// Generated by EcGen at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
			const string snippet_head = @"
#pragma once

#define CMD_ENTER_STANDBY 0x01
#define CMD_ENTER_CYCLE   0x02

#define STATUS_INIT          0x00
#define STATUS_ENTER_STANDBY 0x01
#define STATUS_EXIT_STANDBY  0x02
#define STATUS_STANDBY       0x03
#define STATUS_CYCLE_RUN     0x04

#include <array>
#include <cstdint>

namespace icnc_rpc_controller {

#pragma pack(push, 1)

";
			const string snippet_end = @"

#pragma pack(pop)

}  // namespace icnc_rpc_controller";
			var a2r = PdoA2rServoStructs + '\n' + PdoA2rIoStructs + '\n' + PdoA2rServoGrpHpp + '\n' + PdoA2rIoGrpHpp + '\n' + PdoA2r + '\n' + A2r;
			var r2a = PdoR2aServoStructs + '\n' + PdoR2aIoStructs + '\n' + PdoR2aServoGrpHpp + '\n' + PdoR2aIoGrpHpp + '\n' + PdoR2a + '\n' + R2a;

			return generated_comment + snippet_head + a2r + "\n\n\n" + r2a + snippet_end;

		}

	}
	public string SlaveConfigH
	{
		get
		{
			var generated_comment = $"// Generated by EcGen at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\n";

			var snippet_head = @"
#pragma once
#include <master.h>
#include ""msgs.h""

";

			var variables = this.VariableNamesToExport.Select(x => x.StartsWith("ec") ? $"extern const {x};" : $"extern {x};").Aggregate((a, b) => a + "\n" + b);
			var snippet_foot = @"

extern ec_domain_t*             ancc_domain_rx_domain;
extern ec_domain_t*             ancc_domain_tx_domain;

typedef struct rpmsg_pkg {
    void * rpmsg_buffer;
} rpmsg_pkg_t;

void rpmsg_kfifo_put(rpmsg_pkg_t pkg);
void rpmsg_kfifo_peek(rpmsg_pkg_t* pkg);
void rpmsg_kfifo_get(rpmsg_pkg_t* pkg);

int rpmsg_pdo_get(pdo_a2r_t** pdo);
int rpmsg_pdo_peek(pdo_a2r_t** pdo);
int rpmsg_pdo_get_keep_last(pdo_a2r_t** pdo);

";
			var define_count = $@"
#define SLAVE_CONFIG_COUNT {this.Slaves.Count}
";

			return generated_comment + snippet_head + variables + snippet_foot + define_count;


		}

	}
	public string SlaveConfigPtrs
	{
		get
		{
			var slaveConfigs = Slaves.Select(x => $"static ec_slave_config_t* {x.ConfigName} = NULL;").Aggregate((a, b) => a + "\n" + b);
			var slaveDebug = @"
static ec_slave_config_t* slave_debug = NULL;";
			return Debug ? slaveConfigs + slaveDebug : slaveConfigs;
		}
	}

	public string AppConfigSlave
	{
		get
		{
			var snippet_0 = $@"


int app_config_slave(struct ancc_app* app) {{
	static int config_idx = 0;
	ec_master_t* master = app->platform->ec_master;
";

			var configs = Slaves.Select(x => x.AppConfigSlaveSnippet).Aggregate((a, b) => a + "\n" + b);
			var count = Debug ? Slaves.Count + 1 : Slaves.Count;
			var debug_board_config = $@"
	if (config_idx == {Slaves.Count}){{
	    slave_debug = ecrt_master_slave_config(master,0,{Slaves.Count},0xB95,0x10200);
	    if (slave_debug == NULL) {{
	        return -1;
	    }}
	}}";

			var snippet_1 = $@"
	config_idx++;
	return !(config_idx=={count}) ;
}}

";
			return Debug ?
			snippet_0 + configs + debug_board_config + snippet_1 :
			snippet_0 + configs + snippet_1;

		}

	}

	public string AppEtherCATConfigDc
	{
		get
		{
			var snippet_0 = $@"
int app_ethercat_config_dc(struct ancc_app* app) {{
	ancc_cat_t* cat = app->platform->ancc_cat;
";

			var configs = Slaves.Select(x => x.AppEtherConfigDcSnippet).Aggregate((a, b) => a + b);
			var slave_debug = @"
        ecrt_slave_config_dc(slave_debug, EC_DC_ACTIVATE, EC_DC_CYCLE_TIME, 0, 0, 0);
			";

			var snippet_1 = $@"
        ancc_cat_set_driftcomp_param(cat, DRIFT_COMP_kP, DRIFT_COMP_kI, DRIFT_COMP_kD);
        ancc_cat_set_cycle_time(cat, EC_DC_CYCLE_TIME);
        ancc_cat_set_shift_time(cat, EC_DC_SHIFT_TIME);

    	return 0;
}}
";
			return Debug ?
			snippet_0 + configs + slave_debug + snippet_1 :
			snippet_0 + configs + snippet_1;
		}

	}
	public string AppCreateDomain
	{
		get
		{

			const string snippet_0 = $@"
ec_domain_t* ancc_domain_rx_domain = NULL;
ec_domain_t* ancc_domain_tx_domain = NULL;

int app_create_domain(struct ancc_app* app) {{
    ec_master_t* master = app->platform->ec_master;
    static int domain_idx = 0;
    if (domain_idx == 0) {{
        ancc_domain_rx_domain = ecrt_master_create_domain(master);
        if (ancc_domain_rx_domain == NULL) {{
            return -1;
        }}
    }} else if (domain_idx == 1) {{
        ancc_domain_tx_domain = ecrt_master_create_domain(master);
        if (ancc_domain_tx_domain == NULL) {{
            return -1;
        }}
    }}
    domain_idx++;
 
    return !(domain_idx==2);
}}

";
			return snippet_0;

		}

	}
	public string AppDomainBindPdoEntry
	{
		get
		{
			string snippet_0 = $@"
int app_domain_bind_pdo_entry(struct ancc_app* app) {{
    static int domain_pdo_pairs = 0;
 
    if (domain_pdo_pairs == 0) {{
        if (ecrt_domain_reg_pdo_entry_list(ancc_domain_rx_domain, pdo_rx_group_0)) {{
            xil_printf(""Failed to register RxPDOs.\r\n"");
            return -1;
        }}
    }} else if (domain_pdo_pairs == 1) {{
        if (ecrt_domain_reg_pdo_entry_list(ancc_domain_tx_domain, pdo_tx_group_0)) {{
            xil_printf(""Failed to register TxPDOs.\r\n"");
            return -1;
        }}
    }}

    domain_pdo_pairs++;
    return !(domain_pdo_pairs==2);
}}

";
			return snippet_0;

		}

	}
	public string AppkFifoMsg
	{
		get
		{
			string snippet_0 = @"
DEFINE_KFIFO(rpmsg_kfifo, rpmsg_pkg_t, 64);
static uint16_t used_idx = 0;

void rpmsg_kfifo_put(rpmsg_pkg_t pkg) { kfifo_put(&rpmsg_kfifo, pkg); }

void rpmsg_kfifo_peek(rpmsg_pkg_t* pkg) { kfifo_peek(&rpmsg_kfifo, pkg); }

void rpmsg_kfifo_get(rpmsg_pkg_t* pkg) { kfifo_get(&rpmsg_kfifo, pkg); }

int rpmsg_pdo_get(pdo_a2r_t** pdo) {
    rpmsg_pkg_t pkg = {.rpmsg_buffer = NULL};

    if (kfifo_is_empty(&rpmsg_kfifo)) {
        *pdo     = NULL;
        used_idx = 0;
        return 0;
    }

    kfifo_peek(&rpmsg_kfifo, &pkg);
    a2r_t* a2r_ptr = (a2r_t*)pkg.rpmsg_buffer;
    *pdo           = (pdo_a2r_t*)(&(a2r_ptr)->data[used_idx]);

    used_idx++;
    if (used_idx == a2r_ptr->pkg_num) {
        kfifo_get(&rpmsg_kfifo, &pkg); // remove the pkg from kfifo
        rpmsg_release_rx_buffer(pdo_channel, pkg.rpmsg_buffer);
        used_idx = 0;
    }
    return kfifo_len(&rpmsg_kfifo);
}

int rpmsg_pdo_peek(pdo_a2r_t** pdo) {
    rpmsg_pkg_t pkg = {.rpmsg_buffer = NULL};

    if (kfifo_is_empty(&rpmsg_kfifo)) {
        *pdo     = NULL;
        used_idx = 0;
        return 0;
    }

    kfifo_peek(&rpmsg_kfifo, &pkg);
    a2r_t* a2r_ptr = (a2r_t*)pkg.rpmsg_buffer;
    *pdo           = (pdo_a2r_t*)(&(a2r_ptr)->data[used_idx]);

    return kfifo_len(&rpmsg_kfifo);
}

int rpmsg_pdo_get_keep_last(pdo_a2r_t** pdo) {
    rpmsg_pkg_t pkg = {
        .rpmsg_buffer = NULL,
    };
    int fifo_len = 0;

    if (kfifo_is_empty(&rpmsg_kfifo)) {
        *pdo     = NULL;
        used_idx = 0;
        return 0;
    }

    kfifo_peek(&rpmsg_kfifo, &pkg);
    a2r_t* a2r_ptr = (a2r_t*)pkg.rpmsg_buffer;
    *pdo           = (pdo_a2r_t*)(&(a2r_ptr)->data[used_idx]);
    used_idx++;

    if (used_idx == a2r_ptr->pkg_num) {
        fifo_len = kfifo_len(&rpmsg_kfifo);
        if (fifo_len > 1) {
            kfifo_get(&rpmsg_kfifo, &pkg); // remove the pkg from kfifo
            rpmsg_release_rx_buffer(pdo_channel, pkg.rpmsg_buffer);
            used_idx = 0;
            fifo_len--;
        } else {
            used_idx--;
        }
    }
    return fifo_len;
}

";
			return snippet_0;
		}
	}
	public string SlaveConfig
	{
		get
		{
			var generated_comment = $"// Generated by EcGen at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\n";
			const string snippet_head = @"
#include <stdint.h>
#include ""slave_config_impl.h""
#include ""app.h""
#include ""kfifo/kfifo.h""
#include ""msgs.h""
#include ""openamp/rpmsg.h""
#include ""platform_info.h""
#include ""rproc/rpmsg_ept.h""
#include ""slave_config.h""


";

			return generated_comment + snippet_head + PdoEntryRegs + SyncConfig + SlaveConfigPtrs + AppConfigSlave + AppEtherCATConfigDc + AppCreateDomain + AppDomainBindPdoEntry + AppkFifoMsg;
		}

	}



}

// public static List<EcInfo.>