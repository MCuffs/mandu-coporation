"""Progression and ad-value simulation for the v5 structure plan.

Abstract model (not the combat simulation): a stage is won with a probability that depends on
dad's power (CP) against the stage's recommended power. Coins buy upgrades, lunchboxes add gear,
offline time pays out between sessions, and three player profiles use rewarded ads differently.

Run:  python3 Tools/progression_sim.py            -> prints the report
      python3 Tools/progression_sim.py --write    -> also writes Design/dad-progression-sim.txt
All numbers are first-pass hypotheses to be replaced by measured values after launch.
"""
import json
import math
import random
import sys
from pathlib import Path

# Single source of truth shared with the game (DadEconomy.cs loads the same file).
ECO = json.loads((Path(__file__).resolve().parents[1] / "Assets/DadSchoolBoss/Resources/DadEconomy.json").read_text(encoding="utf-8"))

# ---- content -------------------------------------------------------------------------------
FLOORS = ECO["floors"] + 1      # one floor beyond the shipped content to see the curve continue
PER_FLOOR = ECO["stagesPerFloor"]  # stage 10 = mid-boss (an elite reuse of a regular enemy), stage 20 = floor boss
STAGES = FLOORS * PER_FLOOR


def stage_no(stage):
    return (stage - 1) % PER_FLOOR + 1


def is_boss(stage):
    return stage_no(stage) == PER_FLOOR


def is_mid(stage):
    return stage_no(stage) == PER_FLOOR // 2

# ---- difficulty (sawtooth: rises within a floor, boss spike, relief on the next floor) --------
REQ_BASE, REQ_GROWTH = ECO["requiredBase"], ECO["requiredGrowth"]
BOSS_SPIKE = ECO["floorBossSpike"]   # floor boss needs this much more than the curve
MID_SPIKE = ECO["midBossSpike"]      # mid-boss spike
FLOOR_RELIEF = ECO["floorRelief"]    # first stage of a floor needs less than the curve -> "dad is too strong" moment
WIN_SLOPE = ECO["winSlope"]
BASE_POWER = ECO["basePower"]


def required_cp(stage):
    req = REQ_BASE * REQ_GROWTH ** (stage - 1)
    if is_boss(stage):
        req *= BOSS_SPIKE
    elif is_mid(stage):
        req *= MID_SPIKE
    elif stage_no(stage) == 1 and stage > 1:
        req *= FLOOR_RELIEF
    return req


def win_chance(cp, req, revive=False):
    p = 0.5 + WIN_SLOPE * (cp / req - 1.0)   # 50% at equal power, certain at +20%, ~10% at -16%
    if revive:
        p += 0.25                            # "mom's phone call" revive turns many near-misses into wins
    return min(0.99, max(0.03, p))


# ---- economy ---------------------------------------------------------------------------------
UPGRADE_COST_BASE, UPGRADE_COST_GROWTH, UPGRADE_CP = ECO["upgradeCostBase"], ECO["upgradeCostGrowth"], ECO["upgradePower"]   # one shared cost track
COIN_BASE, COIN_GROWTH, BOSS_COIN_MULT = ECO["coinBase"], ECO["coinGrowth"], ECO["bossCoinMult"]
FAIL_COIN_SHARE, FARM_COIN_SHARE = ECO["failCoinShare"], ECO["replayCoinShare"]
BOXES_PER_CLEAR, BOSS_FIRST_CLEAR_BOXES = ECO["boxesPerClear"], ECO["floorBossFirstClearBoxes"]
OFFLINE_CLEARS_PER_HOUR, OFFLINE_BOXES_PER_HOUR, OFFLINE_CAP_H = ECO["offlineClearsPerHour"], ECO["offlineBoxesPerHour"], ECO["offlineCapHours"]
GEAR_LOG_SCALE = ECO["gearLogScale"]
GEAR_SLOTS = 6
GEAR_WEIGHTS, GEAR_FACTOR, GEAR_DISMANTLE = ECO["gearRarityWeights"], ECO["gearRarityFactor"], ECO["gearDismantleShare"]
GEAR_JITTER, GEAR_FIRST_RARITY = ECO["gearJitter"], ECO["gearFirstRarity"]
STAGE_SECONDS, BOSS_SECONDS, OVERHEAD_SECONDS, AD_SECONDS = 45, 75, 20, 30


def coins_for(stage):
    c = COIN_BASE * COIN_GROWTH ** (stage - 1)
    return c * (BOSS_COIN_MULT if is_boss(stage) or is_mid(stage) else 1.0)


def gear_mult(boxes):
    # Design target the rolled gear is calibrated against (DadProgress.GearValueAt): strong early, diminishing later.
    return 1.0 + GEAR_LOG_SCALE * math.log(1.0 + boxes / 10.0)


def gear_value(opened):
    return GEAR_LOG_SCALE * math.log(1.0 + opened / 10.0) / GEAR_SLOTS


# ---- player profiles (daily caps mirror the v4 ad table) --------------------------------------
PROFILES = {
    "광고 안 봄": dict(result_x2=0, offline_x2=False, free_boxes=0, boost=0, revive=False),
    "광고 가끔": dict(result_x2=0, offline_x2=True, free_boxes=1, boost=0, revive=False),
    "광고 적극": dict(result_x2=5, offline_x2=True, free_boxes=5, boost=3, revive=True),
}
STUCK_ASSIST_AFTER, STUCK_ASSIST_STEP, STUCK_ASSIST_CAP = ECO["stuckAssistAfter"], ECO["stuckAssistStep"], ECO["stuckAssistCap"]   # "엄마의 응원"
BOOST_CP = 1.25          # "아빠 각성": +50% attack and coins for 20 minutes ~= +25% effective power
BOOST_MINUTES = 20
SESSIONS = [(9, 10), (13, 10), (20, 12)]   # (start hour, minutes played); overnight gap is capped by OFFLINE_CAP_H


class Player:
    def __init__(self, profile, rng):
        self.p, self.rng = PROFILES[profile], rng
        self.stage, self.coins, self.boxes, self.level = 1, 0.0, 0, 0
        self.gear_rng = random.Random(rng.random())
        self.gear = [None] * GEAR_SLOTS   # (rarity, value) per slot, rolled exactly like DadProgress.OpenLunchbox
        self.attempts = [0] * (STAGES + 2)
        self.fails_in_row = 0
        self.farm_clears = 0
        self.ads_today = 0
        self.ads_total = 0
        self.cleared = set()

    @property
    def cp(self):
        return BASE_POWER * (1 + UPGRADE_CP) ** self.level * (1 + sum(v for _, v in filter(None, self.gear)))

    def dismantle(self, rarity):
        farm = max(1, self.stage - 1)
        return COIN_BASE * COIN_GROWTH ** (farm - 1) * GEAR_DISMANTLE[rarity]

    def add_boxes(self, n):
        # Players open every box at once ("모두 열기"): keep what is better, dismantle the rest.
        for _ in range(n):
            self.boxes += 1
            r = GEAR_FIRST_RARITY if self.boxes == 1 else self.gear_rng.choices(range(len(GEAR_WEIGHTS)), GEAR_WEIGHTS)[0]
            empty = [i for i, g in enumerate(self.gear) if g is None]
            slot = self.gear_rng.choice(empty) if empty else self.gear_rng.randrange(GEAR_SLOTS)
            value = gear_value(self.boxes) * GEAR_FACTOR[r] * (1 + GEAR_JITTER * (self.gear_rng.random() * 2 - 1))
            old = self.gear[slot]
            if old is None or value > old[1]:
                if old is not None:
                    self.coins += self.dismantle(old[0])
                self.gear[slot] = (r, value)
            else:
                self.coins += self.dismantle(r)

    def spend(self):
        while self.coins >= UPGRADE_COST_BASE * UPGRADE_COST_GROWTH ** self.level:
            self.coins -= UPGRADE_COST_BASE * UPGRADE_COST_GROWTH ** self.level
            self.level += 1

    def watch(self):
        self.ads_today += 1
        self.ads_total += 1
        return AD_SECONDS

    def offline(self, hours):
        hours = min(hours, OFFLINE_CAP_H)
        best = max(1, self.stage - 1)
        coins = OFFLINE_CLEARS_PER_HOUR * hours * coins_for(best) / (BOSS_COIN_MULT if is_boss(best) or is_mid(best) else 1)
        boxes = int(OFFLINE_BOXES_PER_HOUR * hours)
        mult = 1
        if self.p["offline_x2"] and hours >= 1:
            mult = 2
            self.watch()
        self.coins += coins * mult
        self.add_boxes(boxes * mult)
        self.spend()

    def session(self, minutes, caps):
        t = 0.0
        if caps["free_boxes"] > 0:
            for _ in range(caps["free_boxes"]):
                self.add_boxes(5)
                t += self.watch()
            caps["free_boxes"] = 0
        boost_left = 0.0
        while t < minutes * 60 and self.stage <= STAGES:
            s = self.stage
            req = required_cp(s)
            cp = self.cp
            if self.fails_in_row >= 2 and s > 1 and cp < req * 0.97:
                # After two losses in a row a real player grinds the previous stage instead of banging on the wall.
                t += STAGE_SECONDS + OVERHEAD_SECONDS
                self.coins += coins_for(s - 1) * FARM_COIN_SHARE / (BOSS_COIN_MULT if is_boss(s - 1) or is_mid(s - 1) else 1)
                self.farm_clears += 1
                self.spend()
                if self.cp >= req * 0.97 or self.farm_clears % 4 == 0:
                    self.fails_in_row = 0
                continue
            if (is_boss(s) or is_mid(s)) and cp < req * 1.1 and caps["boost"] > 0 and boost_left <= 0:
                caps["boost"] -= 1
                t += self.watch()
                boost_left = BOOST_MINUTES * 60
            if boost_left > 0:
                cp *= BOOST_CP
            losses_here = self.attempts[s] - (1 if s in self.cleared else 0)
            cp *= 1 + min(STUCK_ASSIST_CAP, max(0, losses_here - STUCK_ASSIST_AFTER + 1) * STUCK_ASSIST_STEP)
            self.attempts[s] += 1
            won = self.rng.random() < win_chance(cp, req)
            if not won and self.p["revive"] and cp >= req * 0.8:
                t += self.watch()
                won = self.rng.random() < win_chance(cp, req, revive=True)
            dur = (BOSS_SECONDS if is_boss(s) or is_mid(s) else STAGE_SECONDS) + OVERHEAD_SECONDS
            t += dur
            boost_left -= dur
            if won:
                reward = coins_for(s)
                if caps["result_x2"] > 0 and (is_boss(s) or is_mid(s) or self.coins + reward < UPGRADE_COST_BASE * UPGRADE_COST_GROWTH ** self.level):
                    caps["result_x2"] -= 1
                    t += self.watch()
                    reward *= 2
                self.coins += reward
                self.add_boxes(BOXES_PER_CLEAR + (BOSS_FIRST_CLEAR_BOXES if is_boss(s) and s not in self.cleared else 0))
                self.cleared.add(s)
                self.stage += 1
                self.fails_in_row = 0
            else:
                self.coins += coins_for(s) * FAIL_COIN_SHARE
                self.fails_in_row += 1
            self.spend()


def run(profile, days=7, seed=0):
    rng = random.Random(seed)
    pl = Player(profile, rng)
    log = []
    last_end = None
    for day in range(1, days + 1):
        pl.ads_today = 0
        caps = dict(result_x2=pl.p["result_x2"], free_boxes=pl.p["free_boxes"], boost=pl.p["boost"])
        for start, minutes in SESSIONS:
            now = (day - 1) * 24 + start
            if last_end is not None:
                pl.offline(now - last_end)
            pl.session(minutes, caps)
            last_end = now + minutes / 60
        log.append((day, pl.stage, pl.ads_today, pl.cp))
    return pl, log


def label(stage):
    if stage > STAGES:
        return "전부 클리어"
    return "%d-%d" % ((stage - 1) // PER_FLOOR + 1, stage_no(stage))


def report(seeds=200):
    lines = []
    lines.append("진행 시뮬레이션 (추상 모델, 시드 %d개 평균) — Tools/progression_sim.py" % seeds)
    lines.append("하루 3회 접속(10·10·12분), 사이 시간은 방치 보상(최대 %d시간). 층 %d개 × %d스테이지(10번째 중간 보스, %d번째 층 보스)." % (OFFLINE_CAP_H, FLOORS, PER_FLOOR, PER_FLOOR))
    lines.append("")
    lines.append("[권장 전투력 곡선]")
    row = []
    for s in [1, 9, 10, 11, 19, 20, 21, 30, 40, 41, 50, 60]:
        row.append("%s %d" % (label(s), required_cp(s)))
    lines.append("  " + " · ".join(row))
    lines.append("")
    for profile in PROFILES:
        reached = {d: [] for d in range(1, 8)}
        ads = {d: [] for d in range(1, 8)}
        attempts = [0.0] * (STAGES + 2)
        for seed in range(seeds):
            pl, log = run(profile, seed=seed)
            for day, stage, ads_today, cp in log:
                reached[day].append(stage)
                ads[day].append(ads_today)
            for s in range(1, STAGES + 1):
                attempts[s] += pl.attempts[s] / seeds
        lines.append("[%s]" % profile)
        lines.append("  날짜별 도달(중앙값): " + "  ".join("%d일차 %s" % (d, label(sorted(v)[len(v) // 2])) for d, v in reached.items()))
        lines.append("  하루 광고 시청(평균): " + "  ".join("%d일차 %.1f" % (d, sum(v) / len(v)) for d, v in ads.items()))
        walls = [(s, a) for s, a in enumerate(attempts) if 1 <= s <= STAGES and a >= 1.8 and not (is_boss(s) or is_mid(s))]
        lines.append("  보스 외에 평균 1.8회 이상 걸린 스테이지: " + (", ".join("%s(%.1f회)" % (label(s), a) for s, a in walls) if walls else "없음"))
        first_floor = sum(attempts[1:10]) / 9
        bosses = [(s, attempts[s]) for s in range(1, STAGES + 1) if is_boss(s) or is_mid(s)]
        lines.append("  보스 평균 시도: " + "  ".join("%s %.1f회" % (label(s), a) for s, a in bosses))
        lines.append("  1-1~1-9 평균 시도: %.2f회" % first_floor)
        lines.append("")
    return "\n".join(lines)


if __name__ == "__main__":
    text = report()
    print(text)
    if "--write" in sys.argv:
        out = Path(__file__).resolve().parents[1] / "Design" / "dad-progression-sim.txt"
        out.write_text(text + "\n", encoding="utf-8")
        print("written:", out)
