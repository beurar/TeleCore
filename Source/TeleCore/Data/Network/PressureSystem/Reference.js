var width = 20;
var height = 20;
var ups = 60; // updates per second

var bufferSize = 1; // multiple of production/consumption that a entity can store

var modelSelection = {
    pressureModel : null,
    clampModel: null
}

var globalConfig = {
    displayThroughput: {
        value: false,
        desc: "Display source/sink throughput"
    },
    smoothThroughput: {
        value: true,
        desc: "Smooth source output/sink input over time"
    }
}

var pressureModelDef = {
    "Wave Equation": {
        desc: "Wave equation with linear pressure.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            f += (this.pressureFn(t0) - this.pressureFn(t1)) * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            cSquared: {
                value: 0.01,
                range: [0, 1],
                desc: "C-Squared"
            },
            friction: {
                value: 0.001,
                range: [0, 1],
                desc: "Friction"
            }
        }
    },
    "Wave Equation, Non-Linear Pressure": {
        desc: "Wave equation with non-linear pressure.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            f += (this.pressureFn(t0) - this.pressureFn(t1)) * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            return f;
        },
        pressureFn: function(t) {
            var p = t.content / t.maxContent * 100;
            return p <= 60 ? p : 60 + (p-60) * 10;
        },
        config: {
            cSquared: {
                value: 0.01,
                range: [0, 1],
                desc: "C-Squared"
            },
            friction: {
                value: 0.001,
                range: [0, 1],
                desc: "Friction"
            }
        }
    },
    "Wave Equation with damping": {
        desc: "Model that can quickly eliminate waves without relying on friction.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            var dp = this.pressureFn(t0) - this.pressureFn(t1);
            var c;
            if (sign(f) == sign(dp)) c = cfg.cSquared.value;
            else c = cfg.cSquaredDamper.value;
            f += dp * c;
            f *= 1 - cfg.friction.value;
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            cSquared: {
                value: 0.03,
                range: [0, 1],
                desc: "C-Squared"
            },
            cSquaredDamper: {
                value: 0.04,
                range: [0, 1],
                desc: "C-SquaredDamper"
            },
            friction: {
                value: 0,
                range: [0, 1],
                desc: "Friction"
            }
        }
    },
    "Wave Equation with damping 2": {
        desc: "Model that applies additional friction when waves occur.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            var dp = this.pressureFn(t0) - this.pressureFn(t1);
            var counterFlow = sign(f) != sign(dp);
            f += dp * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            if (counterFlow) f *= (1 - Math.min(0.9,(cfg.dampFriction.value * Math.abs(dp)*0.01)));
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            cSquared: {
                value: 0.03,
                range: [0, 1],
                desc: "C-Squared"
            },
            dampFriction: {
                value: 0.01,
                range: [0, 1],
                desc: "Damper Friction"
            },
            friction: {
                value: 0,
                range: [0, 1],
                desc: "Friction"
            }
        }
    },
    "Wave Equation with damping 3": {
        desc: "Model that adds friction by making fluid stick to the pipe surface.",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            var dp = this.pressureFn(t0) - this.pressureFn(t1);
            var src = f > 0 ? t0 : t1;
            // if the level in the source pipe is dropping (compared to
            // the previous frame) apply additional friction to model
            // the fluid 'sticking' to the pipe surface
            var dc = Math.max(0, src.prevContent - src.content);
            f += dp * cfg.cSquared.value;
            f *= 1 - cfg.friction.value;
            f *= (1 - Math.min(0.5,(cfg.dampFriction.value * dc)));
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            cSquared: {
                value: 0.03,
                range: [0, 1],
                desc: "C-Squared"
            },
            dampFriction: {
                value: 0.01,
                range: [0, 1],
                desc: "Damper Friction"
            },
            friction: {
                value: 0,
                range: [0, 1],
                desc: "Friction"
            }
        }
    },
    "Fixed Acceleration": {
        desc: "Fixed acceleration based on sign of pressure difference (as proposed by Quinor)",
        flowFn: function(t0, t1, f) {
            var cfg = this.config;
            f *= cfg.inertia.value;
            f += ((this.pressureFn(t0) - this.pressureFn(t1)) > 0 ? 1 : -1) * cfg.acceleration.value;
            return f;
        },
        pressureFn: function(t) {
            return t.content / t.maxContent * 100;
        },
        config: {
            acceleration: {
                value: 5,
                range: [0, 100],
                desc: "Acceleration"
            },
            inertia: {
                value: 0.9,
                range: [0, 1],
                desc: "Inertia"
            }
        }
    }
}

var FLOW_SPEED = 1;
var FLUID_MOVE = 2;

var clampModelDef = {
    "Quarter Limit": {
        desc: "Limit flow to a quarter of current content (outflow) or remaining space (inflow)",
        clampFn: function(t0, t1, f, what) {
            var cfg = this.config;
            var c, r;
            if (cfg.enforceMinPipe.value) {
                if (f > 0){
                    c = t0.content;
                    f = clampFlow(c, f, 0.25 * c);
                }
                else
                if (f < 0) {
                    c = t1.content;
                    f = -clampFlow(c, -f, 0.25 * c);
                }
            }
            if (cfg.enforceMaxPipe.value) {
                if (f > 0) {
                    r = t1.maxContent - t1.content;
                    f = clampFlow(r, f, 0.25*r);
                } else
                if (f < 0) {
                    r = t0.maxContent - t0.content;
                    f = -clampFlow(r, -f, 0.25*r);
                }
            }
            return f;
        },
        config: {
            enforceMinPipe: {
                value: true,
                desc: "Enforce pipe min content (= 0)"
            },
            enforceMaxPipe: {
                value: true,
                desc: "Enforce pipe max content (= 100)"
            }
        }
    },
    "Connection Count Limit": {
        desc: "Limit flow to (1/connections) of current content (outflow) or remaining space (inflow)",
        clampFn: function(t0, t1, f, what) {
            var cfg = this.config;
            var d0 = 1/Math.max(1,t0.connCount);
            var d1 = 1/Math.max(1,t1.connCount);
            var c, r;
            if (cfg.enforceMinPipe.value) {
                if (f > 0){
                    c = t0.content;
                    f = clampFlow(c, f, d0*c);
                }
                else
                if (f < 0) {
                    c = t1.content;
                    f = -clampFlow(c, -f, d1*c);
                }
            }
            if (cfg.enforceMaxPipe.value) {
                if (f > 0) {
                    r = t1.maxContent - t1.content;
                    f = clampFlow(r, f, d1*r);
                } else
                if (f < 0) {
                    r = t0.maxContent - t0.content;
                    f = -clampFlow(r, -f, d0*r);
                }
            }
            return f;
        },
        config: {
            enforceMinPipe: {
                value: true,
                desc: "Enforce pipe min content (= 0)"
            },
            enforceMaxPipe: {
                value: true,
                desc: "Enforce pipe max content (= 100)"
            }
        }
    },
    "Overcommit Clamping": {
        desc: "Limit flow to a configurable fraction of current content (outflow) or remaining space (inflow)",
        clampFn: function(t0, t1, f, what) {
            var cfg = this.config;
            var d, c, r;
            if (cfg.enforceMinPipe.value) {
                // Limit outflow to 1/divider of fluid content in src pipe     
                if (what == FLOW_SPEED && cfg.maintainFlowSpeed) d = 1;
                else d = 1 / cfg.minDivider.value;
                if (f > 0) {
                    c = t0.content;
                    f = clampFlow(c, f, d*c);
                } else
                if (f < 0) {
                    c = t1.content;
                    f = -clampFlow(c, -f, d*c);
                }
            }
            if (cfg.enforceMaxPipe.value && (what == FLUID_MOVE || !cfg.maintainFlowSpeed.value)) {
                // Limit inflow to 1/divider of remaining space in dst pipe
                d = 1 / cfg.maxDivider.value;
                if (f > 0) {
                    r = t1.maxContent - t1.content;
                    f = clampFlow(r, f, d*r);
                } else
                if (f < 0) {
                    r = t0.maxContent - t0.content;
                    f = -clampFlow(r, -f, d*r);
                }
            }
            return f;
        },
        config: {
            enforceMinPipe: {
                value: true,
                desc: "Enforce pipe min content (= 0)"
            },
            enforceMaxPipe: {
                value: true,
                desc: "Enforce pipe max content (= 100)"
            },
            maintainFlowSpeed: {
                value: false,
                desc: "Do not reduce flow speed when clamping"
            },
            minDivider: {
                value: 4,
                range: [1, 4],
                desc: "Divider for available fluid [1..4]"
            },
            maxDivider: {
                value: 1,
                range: [1, 4],
                desc: "Divider for remaining space [1..4]"
            },
        }
    },
    "Contested Ratio Clamping": {
        desc: "Limit flow to a fair ratio of the contested fluid / space (as proposed by Quinor)",
        clampFn: function(t0, t1, f, what) {
            return f;
        },
        extraClampFn: function(t) {
            var cfg = this.config;
            var ratioOut = 1;
            var ratioIn = 1;
            if (cfg.enforceMinPipe.value) {
                var rOut = 0;
                if (t.flowTop < 0) rOut += -t.flowTop;
                if (t.flowLeft < 0) rOut += -t.flowLeft;
                if (t.bottom && t.bottom.flowTop > 0) rOut += t.bottom.flowTop;
                if (t.right && t.right.flowLeft > 0) rOut += t.right.flowLeft;
                ratioOut = rOut <= t.content ? 1 : t.content / rOut;
            }
            if (cfg.enforceMaxPipe.value) {
                var rIn = 0;
                if (t.flowTop > 0) rIn += t.flowTop;
                if (t.flowLeft > 0) rIn += t.flowLeft;
                if (t.bottom && t.bottom.flowTop < 0) rIn += -t.bottom.flowTop;
                if (t.right && t.right.flowLeft < 0) rIn += -t.right.flowLeft;
                var space = t.maxContent - t.content;
                ratioIn = rIn <= space ? 1 : space / rIn;
            }
            if (t.flowTop < 0) t.moveTop = -Math.min(-t.moveTop, ratioOut * -t.flowTop);
            else t.moveTop = Math.min(t.moveTop, ratioIn * t.flowTop);

            if (t.flowLeft < 0) t.moveLeft = -Math.min(-t.moveLeft, ratioOut * -t.flowLeft);
            else t.moveLeft = Math.min(t.moveLeft, ratioIn * t.flowLeft);

            var tb = t.bottom;
            if (tb) {
                if (tb.flowTop >= 0) tb.moveTop = Math.min(tb.moveTop, ratioOut * tb.flowTop);
                else tb.moveTop = -Math.min(-tb.moveTop, ratioIn * -tb.flowTop);
            }
            var tr = t.right;
            if (tr) {
                if (tr.flowLeft >= 0) tr.moveLeft = Math.min(tr.moveLeft, ratioOut * tr.flowLeft);
                else tr.moveLeft = -Math.min(-tr.moveLeft, ratioIn * -tr.flowLeft);
            }
        },
        extraClamp2Fn: function(t) {
            var cfg = this.config;
            if (!cfg.maintainFlowSpeed) {
                t.flowTop = t.moveTop;
                t.flowLeft = t.moveLeft;
            }
        },
        config: {
            enforceMinPipe: {
                value: true,
                desc: "Enforce pipe min content (= 0)"
            },
            enforceMaxPipe: {
                value: true,
                desc: "Enforce pipe max content (= 100)"
            },
            maintainFlowSpeed: {
                value: true,
                desc: "Do not reduce flow speed when clamping"
            }
        }
    }
}

var NONE = 0;
var PIPE = 1;
var SOURCE = 2;
var SINK = 3;
var TANK = 4;
var EDIT = 5;

var tileSize = 30;
var typeClass = ['none', 'pipe', 'source', 'sink', 'tank', 'edit'];
var pipe = [];
var tile = [];
var tool = -1;
var selectedTile = null;
var underflow = { frame: 0, total: 0, counter: 0, desc: "Underflow (frame/avg)" };
var overflow = { frame: 0, total: 0, counter: 0, desc: "Overflow (frame/avg)" };
var flowClamped = { frame: 0, total: 0, counter: 0, desc: "Flow clamped (frame/avg)" };

var pressureModel;
var clampModel;

var defProp = {
    1: { // PIPE
        maxContent: 100
    },
    2: { // SOURCE
        progress: 0,
        maxContent: 100,
    },
    3: { // SINK
        progress: 180,
        maxContent: 100,
    },
    4: { // TANK
        maxContent: 25000
    }
}

var defTileConfig = {
    1: { // PIPE
    },
    2: { // SOURCE
        cycle: {
            value: 60,
            range: [1,1000000],
            desc: "Cycle duration"
        },
        batchSize: {
            value: 100,
            range: [1,100000000],
            desc: "Production/cycle"
        }
    },
    3: { // SINK
        cycle: {
            value: 180,
            range: [1,1000000],
            desc: "Cycle duration"
        },
        batchSize: {
            value: 100,
            range: [1,100000000],
            desc: "Consumption/cycle"
        }
    },
    4: { // TANK
    }
}

// invoke this method with positive flow and limit values: signed values denoting flow direction
// are not handled correctly
function clampFlow(content, flow, limit) {
    // 'content' can be available fluid or remaining space

    // natural clamp when pipe is empty / no more space
    if (content <= 0)	return 0;
    // natural clamp that would be imposed anyway because fluid/space was running out:
    var n = Math.max(0, flow - content);
    // actual clamp imposed by the clamp method
    var a = Math.max(0, flow - limit);
    // extra clamp as a result of the clamp method
    var x = Math.max(0, a-n);

    if (measureClamp) flowClamped.frame += x;

    return flow <= limit ? flow : limit;
}

$("#toolbar").each(function(i,q) {
    $(typeClass).each(function(j,c){
        var t = $("<div class='tool'><div class='image'></div></div>");
        t.addClass(c);
        $(q).append(t);
        t.on("click", function(e) {
            selectTool(j);
        });
    });
});

$("#exportBtn").on("click", exportSim);
$("#importBtn").on("click", importSim);

(function(){
    var s = $("#selPressureModel");
    $.each(pressureModelDef, function(k,v) {
        s.append("<option>" + k + "</option>");
    });
    s.on("change", function() {
        selectPressureModel(s.prop("value"));
    });
})();

(function(){
    var s = $("#selClampModel");
    $.each(clampModelDef, function(k,v) {
        s.append("<option>" + k + "</option>");
    });
    s.on("change", function() {
        selectClampModel(s.prop("value"));
    });
})();

var globalStats = [overflow, underflow, flowClamped];

$.each(globalStats, function(i,v) {
    var d = $("<div>"+v.desc+" : <span class='display resetBtn'></span></div>")
    $("#globalStats").append(d);
    v.display = d.find("span");
    v.display.on("click", function() {
        v.total = 0;
        v.counter = 0;
    });
});

generateConfigControllers(globalConfig);
displayControls(globalConfig, $("#globalConfig"));
$.each(pressureModelDef, function(k,v) {
    generateConfigControllers(v.config);
});
$.each(clampModelDef, function(k,v) {
    generateConfigControllers(v.config);
});

selectPressureModel("Wave Equation with damping");
selectClampModel("Overcommit Clamping");
selectTool(PIPE);

function selectPressureModel(m) {
    modelSelection.pressureModel = m;
    pressureModel = pressureModelDef[m];
    $("#selPressureModel").prop("value", m);
    selectModelFor($("#cfgPressureModel"), pressureModel);
}

function selectClampModel(m) {
    modelSelection.clampModel = m;
    clampModel = clampModelDef[m];
    $("#selClampModel").prop("value", m);
    selectModelFor($("#cfgClampModel"), clampModel);
}

var modelSelectionFunction = {
    pressureModel : selectPressureModel,
    clampModel : selectClampModel
}

function selectModelFor(jqCfg, model) {
    var jq = jqCfg;
    jq.html("<div class='desc'></div");
    jq.find(".desc").text(model.desc);
    displayControls(model.config, jq);
}

function displayControls(conf, jqDst) {
    $.each(conf, function(k,v) {
        v.input.off();
        if (typeof v.value == 'boolean') {
            v.input.on("click", v.uiCallback);
        }
        if (typeof v.value == 'number') {
            v.input.on("keyup", v.uiCallback);
        }
        jqDst.append(v.uiElement);
    });
}

function generateConfigControllers(conf) {
    $.each(conf, function(k,v) {
        var c;
        var value = v.value;
        if (typeof value == 'boolean') {
            v.uiElement = $("<div>"+v.desc+" <input type='checkbox' /></div>");
            v.input = v.uiElement.find("input");
            v.input.prop("checked", v.value);
            v.uiCallback = function() { v.setTo(v.input.prop("checked")); };
            v.setTo = function(val) {
                v.value = val;
                v.input.prop("checked", val)
            };
        }
        if (typeof value == 'number') {
            v.uiElement = $("<div>"+v.desc+" <input type='text' /></div>");
            v.input = v.uiElement.find("input");
            v.input.prop("value", value);
            v.uiCallback = function() {
                var val = v.input.prop("value") * 1;
                var valid = v.range[0] <= val && val <= v.range[1];
                v.input.toggleClass("invalid", !valid);
                v.setTo(val);
            };
            v.setTo = function(val) {
                var valid = v.range[0] <= val && val <= v.range[1];
                var cur = v.input.prop("value") * 1;
                if (valid) v.value = val;
                if (valid && cur != val) {
                    v.input.prop("value", val);
                    v.input.toggleClass("invalid", !valid);
                }
            };
        }
    });
}

(function() {
    var x,y;

    for (x = 0; x < width; x++) tile[x] = [];

    for (y = 0; y < height; y++) {
        var row = $("<div class='row'></div>");
        $("#grid").append(row);
        for (x = 0; x < width; x++) {
            var div = $("<div class='tile'><div class='icon'></div></div>");
            var fluid = $("<div class='fluid'></div>");
            var flowL = $("<div class='flowLeft'></div>");
            var flowT = $("<div class='flowTop'></div>");
            var tp = $("<div class='throughput'></div>");
            var level = $("<div class='level'></div>");
            var prog = $("<div class='progress'></div>");
            var buf = $("<div class='buffer'></div>");
            row.append(div);
            div.append(fluid);
            div.append(flowL);
            div.append(flowT);
            div.append(tp);
            div.append(level);
            div.append(buf);
            div.append(prog);
            var t = tile[x][y] = {
                type: NONE,
                config: {},
                connCount: 0,
                content : 0,
                prevContent: 0,
                flowLeft : 0,
                flowTop: 0,
                moveLeft : 0,
                moveTop: 0,
                flowRate : 0,
                throughput: { frame: 0, total: 0, counter: 0 },
                buffer: 0,
                blocked: false,
                div : div,
                divFluid : fluid,
                divFlowLeft : flowL,
                divFlowTop: flowT,
                divLevel: level,
                divTp: tp,
                divProgress: prog,
                divBuffer: buf
            };
            (function(tl) {
                div.on("mousedown mouseenter", function(e) {
                    if (e.originalEvent.buttons == 1) {
                        if (tool == EDIT) selectTile(tl);
                        else changeTile(tl, tool);
                    }
                });
            })(t);
        }
    }
    for (y = 0; y < height; y++) {
        for (x = 0; x < width; x++) {
            if (y > 0 ) tile[x][y].top = tile[x][y-1];
            if (y < height-1 ) tile[x][y].bottom = tile[x][y+1];
            if (x > 0 ) tile[x][y].left = tile[x-1][y];
            if (x < width-1 ) tile[x][y].right = tile[x+1][y];
        }
    }
})();

// set some initial pipes 
for (x = 3; x <= width - 4; x++) changeTile(tile[x][9], PIPE);
for (y = 4; y <= 14; y++) changeTile(tile[2][y], PIPE);
for (y = 4; y <= 14; y++) changeTile(tile[width-3][y], PIPE);
for (y = 7; y <= 9; y++) changeTile(tile[9][y], PIPE);
changeTile(tile[1][4], SOURCE);
changeTile(tile[1][9], SOURCE);
changeTile(tile[1][14], SOURCE);
changeTile(tile[width-2][4], SINK);
changeTile(tile[width-2][9], SINK);
changeTile(tile[width-2][14], SINK);
changeTile(tile[9][6], TANK);

var startTime = getTime();
var frame = 0;
var lastCnt = startTime;
var upsCnt = 0;
var fpsCnt = 0;
var avgFps = 0;
var avgUps = 0;
var measureClamp = false;

window.setInterval(function() { update() }, 1000/ups);
function update() {
    var time = getTime();
    fpsCnt++;
    if (time - lastCnt > 500) {
        $("#upsDisplay").text(Math.round(upsCnt / (time - lastCnt) * 1000));
        $("#fpsDisplay").text(Math.round(fpsCnt / (time - lastCnt) * 1000));
        upsCnt = 0;
        fpsCnt = 0;
        lastCnt = time;
    }
    var expFrame = (time - startTime)/ 1000 * ups;
    var x,y;
    while (frame < expFrame) {
        upsCnt++;
        frame++;
        $.each(globalStats, function(i,v) {
            v.total += v.frame;
            v.frame = 0;
            v.counter++;
        });
        for (y = 0; y < height; y++) {
            for (x = 0; x < width; x++) {
                updateFlow(x,y);
            }
        }
        if (clampModel.extraClampFn) {
            for (y = 0; y < height; y++) {
                for (x = 0; x < width; x++) {
                    t = tile[x][y];
                    if (t.type == NONE) continue;
                    clampModel.extraClampFn(t);
                }
            }
        }
        if (clampModel.extraClamp2Fn) {
            for (y = 0; y < height; y++) {
                for (x = 0; x < width; x++) {
                    t = tile[x][y];
                    if (t.type == NONE) continue;
                    clampModel.extraClampFn(t);
                }
            }
        }
        for (y = 0; y < height; y++) {
            for (x = 0; x < width; x++) {
                t = tile[x][y];
                t.prevContent = t.content;
            }
        }
        for (y = 0; y < height; y++) {
            for (x = 0; x < width; x++) {
                updateContent(x,y);
            }
        }
        for (y = 0; y < height; y++) {
            for (x = 0; x < width; x++) {
                updateActiveTile(tile[x][y]);
            }
        }
    }
    for (y = 0; y < height; y++) {
        for (x = 0; x < width; x++) {
            t = tile[x][y];
            if (t.content < 0) underflow.frame -= t.content;
            var of = t.content - t.maxContent;
            if (of > 0) overflow.frame += of;
            updateFlowRate(t);
            if (t.type != NONE) draw(t);
        }
    }
    $.each(globalStats, function(i,v) {
        v.display.text(round(v.frame,0) + " / " + round(v.total/v.counter,2));
    });
    updateDisplay();
}

function updateFlow(x, y) {
    var pMid = tile[x][y];
    if (pMid.type == NONE) return;
    var f;
    if (y > 0) {
        var pTop = tile[x][y-1];
        if (pTop.type != NONE) {
            f = pMid.flowTop;
            f = pressureModel.flowFn(pTop, pMid, f);
            pMid.flowTop = clampModel.clampFn(pTop, pMid, f, FLOW_SPEED);
            measureClamp = true;
            pMid.moveTop = clampModel.clampFn(pTop, pMid, f, FLUID_MOVE);
            measureClamp = false;
        } else {
            pMid.flowTop = 0;
            pMid.moveTop = 0;
        }
    }
    if (x > 0) {
        var pLeft = tile[x-1][y];
        if (pLeft.type != NONE) {
            f = pMid.flowLeft;
            f = pressureModel.flowFn(pLeft, pMid, f);
            pMid.flowLeft = clampModel.clampFn(pLeft, pMid, f, FLOW_SPEED);
            measureClamp = true;
            pMid.moveLeft = clampModel.clampFn(pLeft, pMid, f, FLUID_MOVE);
            measureClamp = false;
        } else {
            pMid.flowLeft = 0;
            pMid.moveLeft = 0;
        }
    }
}

function updateContent(x, y) {
    var pMid = tile[x][y];
    if (pMid.type == NONE) return;
    if (y > 0) {
        var pTop = tile[x][y-1];
        if (pTop.type != NONE) {
            pTop.content -= pMid.moveTop;
            pMid.content += pMid.moveTop;
        }
    }
    if (x > 0) {
        var pLeft = tile[x-1][y];
        if (pLeft.type != NONE) {
            pLeft.content -= pMid.moveLeft;
            pMid.content += pMid.moveLeft;
        }
    }
}

function updateFlowRate(t) {
    if (t.type == NONE) {
        t.flowRate = 0;
        return;
    }
    var fp = 0;
    var fn = 0;
    var add = function(f) { if (f > 0) fp+=f; else fn-=f;};
    add(t.moveTop);
    add(t.moveLeft);
    if (t.right) add(-t.right.moveLeft);
    if (t.bottom) add(-t.bottom.moveTop);
    t.flowRate = Math.max(fp,fn);
}

function draw(p) {
    var maxFlowRate = 3000 / 60;
    var opc = Math.min(Math.abs(p.content / p.maxContent), 1);
    p.divFluid.css("opacity", opc);
    if (p.content > p.maxContent) p.divFluid.css("background-color", "#37f");
    else p.divFluid.css("background-color", p.content > 0 ? "#07f" : "#f30");
    var f =  Math.min(1,Math.log(1 + Math.abs(p.moveTop)/maxFlowRate*1.71828)) * 0.4 * tileSize;
    p.divFlowTop.css("height", f + "px");
    p.divFlowTop.css("top", p.moveTop > 0 ? "0px" : (-f-1) + "px")
    f = Math.min(1,Math.log(1 + Math.abs(p.moveLeft)/maxFlowRate*1.71828)) * 0.4 * tileSize;
    p.divFlowLeft.css("width", f + "px");
    p.divFlowLeft.css("left", p.moveLeft > 0 ? "0px" : (-f-1) + "px")

    p.divLevel.css("width", Math.max(0,Math.min(1, p.content / p.maxContent)) * tileSize + "px");

    if (p.type == SOURCE || p.type == SINK) {
        var prog = Math.min(1, p.progress / p.config.cycle.value);
        var bufSize = p.config.batchSize.value * bufferSize;
        var buf = Math.min(1, p.buffer/bufSize);
        p.divProgress.css("width", prog * tileSize + "px");
        p.divProgress.toggleClass("blocked", t.blocked);
        p.divBuffer.css("width", buf * tileSize + "px");
        if (globalConfig.displayThroughput.value)
            p.divTp.text(Math.round(p.throughput.frame * ups));
        else
            p.divTp.text("");
    }
}

function updateActiveTile(t) {
    var f = 0;
    var m;
    var r;
    if (t.type == SOURCE) {
        if (t.progress < t.config.cycle.value)	t.progress++;
        if (t.progress >= t.config.cycle.value) {
            if (t.buffer <= (bufferSize-1)*t.config.batchSize.value) {
                t.buffer += t.config.batchSize.value;
                t.progress = 0;
                t.blocked = false;
            } else {
                t.blocked = true;
            }
        }
        m = t.buffer; // amount to take out of buffer
        if (globalConfig.smoothThroughput.value) {
            r = t.config.cycle.value - t.progress;
            if (r < 1) r = 1;
            m = m / r;
        }
        var s = 100 - t.content; // space remaining in pipe
        f = Math.max(0, Math.min(m, s));
        t.content += f;
        t.buffer -= f;
    } else if (t.type == SINK) {
        if (t.progress < t.config.cycle.value)	t.progress++;
        if (t.progress >= t.config.cycle.value) {
            if (t.buffer >= t.config.batchSize.value) {
                t.buffer -= t.config.batchSize.value;
                t.progress = 0;
                t.blocked = false;
            } else {
                t.blocked = true;
            }
        }
        m = (bufferSize*t.config.batchSize.value)-t.buffer; // amount to take out of pipe
        if (globalConfig.smoothThroughput.value) {
            r = t.config.cycle.value - t.progress;
            if (r < 1) r = 1;
            m = m / r;
        }
        f = Math.min(m, Math.max(0, t.content));
        t.content -= f;
        t.buffer += f;
    }
    t.throughput.frame = f;
    t.throughput.total += f;
    t.throughput.counter++;
}

function changeTile(tile, type) {
    if (tile.type == type) return;
    tile.div.removeClass(typeClass[tile.type]);
    tile.type = type;
    tile.config = {};
    tile.content = 0;
    this.prevContent = 0;
    tile.flowTop = 0;
    tile.flowLeft = 0;
    tile.moveTop = 0;
    tile.moveLeft = 0;
    tile.throughput = { frame: 0, total: 0, counter: 0 };
    tile.cycle = 0;
    tile.progress = 0;
    tile.div.addClass(typeClass[type]);
    tile.divTp.text("");
    tile.divProgress.css("width", "0px");
    tile.divBuffer.css("width", "0px");
    tile.divFlowTop.css("height", "0px");
    tile.divFlowLeft.css("width", "0px");
    tile.divLevel.css("width", "0px");
    var def = defProp[type];
    for (var k in def)
        tile[k] = def[k];
    $.each(defTileConfig[type], function(k,c) {
        var d = tile.config[k] = {};
        $.each(c, function(k, v) {
            d[k] = v;
        });
    });
    generateConfigControllers(tile.config);
    if (type == NONE) {
        draw(tile);
    }
    $.each([tile, tile.left, tile.right, tile.top, tile.bottom], function(i,t) {
        if (t == null) return;
        var n = 0;
        if (t.type != NONE) {
            if (t.left && t.left.type != NONE) n++;
            if (t.right && t.right.type != NONE) n++;
            if (t.top && t.top.type != NONE) n++;
            if (t.bottom && t.bottom.type != NONE) n++;
        }
        t.connCount = n;
    });
}

function clearTiles() {
    var x,y;
    for (y = 0; y < height; y++)
        for (x = 0; x < width; x++)
            changeTile(tile[x][y], NONE);
}

function selectTile(tile) {
    if (tile.type == NONE) return;
    if (selectedTile)
        selectedTile.div.removeClass("selected");
    tile.div.addClass("selected");
    selectedTile = tile;
    var jqControls = $("#cfgEntity");
    jqControls.html("<div>Fluid content: <span class='content'></span></div><div>Flow Rate: <span class='flowRate'></span></div><div>Buffer: <span class='buffer'></span></div><div>Progress: <span class='progress'> %</span></div><div>Throughput (frame/avg): <span class='display resetBtn'><span class='throughput'></span> / <span class='throughputAvg'></span></span></div>");
    var type = selectedTile.type;
    displayControls(tile.config, jqControls);
    jqControls.find(".resetBtn").on("click", function() {
        if (selectedTile) {
            selectedTile.throughput.total = selectedTile.throughput.frame;
            selectedTile.throughput.counter = 1;
        }
    });
}

function updateDisplay() {
    if (!selectedTile) return;
    var jqControls = $("#cfgEntity");
    jqControls.find(".content").text(Math.round(selectedTile.content));
    var s = "net: " + Math.round(selectedTile.flowRate*ups);
    if (selectedTile.top.type != NONE) s += " top: " +Math.round(selectedTile.moveTop*ups);
    if (selectedTile.left.type != NONE) s += " left: " +Math.round(selectedTile.moveLeft*ups);
    if (selectedTile.bottom && selectedTile.bottom.type != NONE) s += " bottom: " +Math.round(-selectedTile.bottom.moveTop*ups);
    if (selectedTile.right && selectedTile.right.type != NONE) s += " right: " +Math.round(-selectedTile.right.moveLeft*ups);
    jqControls.find(".flowRate").text(s);
    var hasBuffer = selectedTile.config && selectedTile.config.batchSize;
    var b = hasBuffer ? Math.round(selectedTile.buffer) : "-";
    jqControls.find(".buffer").text(b);
    var hasCycle = selectedTile.progress && selectedTile.config && selectedTile.config.cycle;
    var p = hasCycle ? Math.round(selectedTile.progress/Math.max(1,selectedTile.config.cycle.value)*100) : "-";
    jqControls.find(".progress").text(p);
    jqControls.find(".throughput").text(Math.round(selectedTile.throughput.frame*ups));
    jqControls.find(".throughputAvg").text(Math.round(selectedTile.throughput.total/selectedTile.throughput.counter*ups));
}

function selectTool(t) {
    $(typeClass).each(function(j,c){
        var q = $("#toolbar .tool." + c);
        if (j == t) q.addClass("selected");
        else q.removeClass("selected");
    });
    tool = t;
}

function exportSim() {
    var e = {
        version: 1,
        modelSelection: {},
        globalConfig: {},
        pressureModelConfig: {},
        clampModelConfig: {}
    };
    $.each(modelSelection, function(k,v) {e.modelSelection[k] = v;});
    $.each(globalConfig, function(k,v) {e.globalConfig[k] = v.value;});
    $.each(pressureModelDef, function(k,v) {
        var c = e.pressureModelConfig[k] = {};
        $.each(v.config, function(k,v) {c[k]=v.value;});
    });
    $.each(clampModelDef, function(k,v) {
        var c = e.clampModelConfig[k] = {};
        $.each(v.config, function(k,v) {c[k]=v.value;});
    });
    var grd = e.grid = [];
    var x,y;
    var row;
    var cnt;
    for (y = 0; y < height; y++) {
        row = [];
        cnt = 0;
        for (x = 0; x < width; x++) {
            var t = tile[x][y];
            if (t.type == NONE) {
                row.push(0);
            } else {
                cnt++;
                var c = {};
                c.type = t.type;
                if (Object.keys(t.config).length > 0) {
                    c.config = {};
                    $.each(t.config, function(k,v) {
                        c.config[k] = v.value;
                    });
                }
                if (Object.keys(c).length == 1)
                    row.push(t.type);
                else
                    row.push(c);
            }
        }
        grd.push(cnt == 0 ? [] : row);
    }
    var json = JSON.stringify(e);
    $("#exchangeBox").val(json);
}

function importSim() {
    var json = $("#exchangeBox").val();
    var e = JSON.parse(json);
    $.each(e.modelSelection, function(k,v) { modelSelectionFunction[k](v);});
    $.each(e.globalConfig, function(k,v) {globalConfig[k].setTo(v);});
    $.each(pressureModelDef, function(k,v) {
        var c = e.pressureModelConfig[k];
        if (c) $.each(v.config, function(k,v) { v.setTo(c[k]); });
    });
    $.each(clampModelDef, function(k,v) {
        var c = e.clampModelConfig[k];
        if (c) $.each(v.config, function(k,v) {v.setTo(c[k]);});
    });
    clearTiles();
    var grd = e.grid;
    var x,y;
    var row;
    var cnt;
    for (y = 0; y < height; y++) {
        row = e.grid[y];
        if (row.length == 0) continue;
        for (x = 0; x < width; x++) {
            var t = row[x];
            if (typeof t == 'number') {
                changeTile(tile[x][y], t);
            } else {
                changeTile(tile[x][y], t.type);
                if (t.config != null && Object.keys(t.config).length > 0) {
                    $.each(t.config, function(k,v) {
                        tile[x][y].config[k].setTo(v);
                    });
                }
            }
        }
        grd.push(cnt == 0 ? [] : row);
    }
    if (selectedTile) selectTile(selectedTile);
}

function round(v, decimals) {
    var p = Math.pow(10,decimals);
    return Math.round(v*p)/p;
}

function sign(v) {
    return v > 0 ? 1 : (v == 0 ? 0 : -1);
}

function getTime() {
    return (new Date()).getTime();
}


