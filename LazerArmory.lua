local Addon = ...

local SLOT_FROM = 1;
local SLOT_TO = 19;
local PING_TOLERANCE = 400;

local function Timestamp()
	return date("%y-%m-%dT%H:%M:%S");
end

function SerializeLazerArmoryConfig()
	local str = "{";
	
	function safe(str) if str ~= nil then return string.gsub(str, "\"", ""); else return nil; end; end	
	function quote(str) if str ~= nil then return "\"" .. safe(str) .. "\""; else return "null" end; end
	function keyAndStr(key, val) return quote(key) .. ":" .. quote(val)	end	
	function keyAndInt(key, val) return quote(key) .. ":" .. val end
	
	str = str .. keyAndStr("name", LazerArmoryGear["Name"]) .. ",";
	str = str .. keyAndStr("realm", LazerArmoryGear["Realm"]) .. ",";
	str = str .. keyAndInt("classId", LazerArmoryGear["Class"]) .. ",";
	str = str .. keyAndStr("saved", LazerArmoryGear["Saved"]) .. ",";
	str = str .. keyAndStr("guild", LazerArmoryGear["Guild"]) .. ",";
	str = str .. keyAndStr("guildRank", LazerArmoryGear["GuildRank"]) .. ",";
	
	str = str .. quote("archive") .. ":{"
	local first = true;
	for k, v in pairs(LazerArmoryGear["Archive"]) do
		if not first then str = str .. ","; end;
		first = false;
		str = str .. keyAndStr(k, v);
	end
	str = str .. "},";
	
	str = str .. quote("spec1") .. ":["
	first = true;
	for i = SLOT_FROM, SLOT_TO do
		local v = LazerArmoryGear["Spec1"][i]
		if not first then str = str .. ","; end;
		first = false;
		if v ~= nil then str = str .. quote(v) else str = str .. "null" end
	end
	str = str .. "],"
	
	str = str .. quote("spec2") .. ":["
	first = true;
	for i = SLOT_FROM, SLOT_TO do
		local v = LazerArmoryGear["Spec2"][i]
		if not first then str = str .. ","; end;
		first = false;
		if v ~= nil then str = str .. quote(v) else str = str .. "null" end
	end
	str = str .. "]"

	str = str .. "}"
	return str;
end

function EncodeLazerArmory(data)
	local b='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
    return ((data:gsub('.', function(x) 
        local r,b='',x:byte()
        for i=8,1,-1 do r=r..(b%2^i-b%2^(i-1)>0 and '1' or '0') end
        return r;
    end)..'0000'):gsub('%d%d%d?%d?%d?%d?', function(x)
        if (#x < 6) then return '' end
        local c=0
        for i=1,6 do c=c+(x:sub(i,i)=='1' and 2^(6-i) or 0) end
        return b:sub(c+1,c+1)
    end)..({ '', '==', '=' })[#data%3+1])
end

local function PrepareAddon()
	-- ADDON_LOADED has been fired by now. All variables loaded.
	if LazerArmoryGear == nil then
		-- This is the first time someone is running the addon.
		guildName, guildRankName, guildRankIndex = GetGuildInfo("player");
		LazerArmoryGear = {};
		LazerArmoryGear["Spec1"] = {} -- First spec
		LazerArmoryGear["Spec2"] = {} -- Second spec
		LazerArmoryGear["Archive"] = {} -- Old / unused gear
		local name, _ = UnitName("player");
		local realm = GetRealmName();
		local localizedClass, englishClass, classIndex = UnitClass("player");
		LazerArmoryGear["Name"] = name;
		LazerArmoryGear["Realm"] = realm;
		LazerArmoryGear["Class"] = classIndex;
		LazerArmoryGear["Saved"] = Timestamp();
		LazerArmoryGear["Guild"] = guildName;
		LazerArmoryGear["GuildRank"] = guildRankName;
	end
	
	-- Register a slash command so that Regrin is happy and can export the config manually:
	SLASH_LAZERARMORY1 = "/la";
	SLASH_LAZERARMORY2 = "/lka";
	SLASH_LAZERARMORY3 = "/lazerarmory";
	SLASH_LAZERARMORY4 = "/lazerkittensarmory";
	SLASH_LAZERARMORY5 = "/regrin";
	SlashCmdList["LAZERARMORY"] = function(msg, editbox)
		local json = SerializeLazerArmoryConfig()
		local base = EncodeLazerArmory(json)		
		StaticPopupDialogs["LAZERARMORY"] = {
			text = "Copy-paste this for manual export:",
			button1 = "Close",
			timeout = 30,
			whileDead = true,
			hideOnEscape = true,
			hasEditBox = true,
			maxLetters = 50000,
			OnShow = function (self, data)
				self.editBox:SetText(base)
			end
		};
		StaticPopup_Show("LAZERARMORY");
	end;
	-- The client app shoul write into our addon's config file a "ping" number
	-- in intervals. This number is the number of seconds since epoch (on POSIX systems).
	
	-- We can compare this number (if it even exists) with the result of time() 
	-- function to determine if the client app is (probably) running or not.
	-- It's a rough way of doing this but there is no other alternative.		
	local uptime = time();
	if (LazerArmoryPing == nil) or ((LazerArmoryPing + PING_TOLERANCE) < uptime) then
		C_Timer.After(30, function() 
			print("The LazerArmory client app is probably not running!");
			print("Please make sure to run the app, othervise your gear won't be saved on the Lazer Kittens server.");
			print("To check again whether the app is running or not, do a /reload.");
		end)
	else
		C_Timer.After(30, function()
			print("Lazer Kittens' Armory app was detected correctly.");
		end)
	end
end

local function SaveGear(self, event, ...)
	-- Initialize our addon environment:
	if event == "ADDON_LOADED" and select(1, ...) == Addon then
		PrepareAddon();
	end

	local spec = GetActiveTalentGroup();
	-- Iterate through all equipped items - see SlotId documentation:
	local ready = false;
	for i = SLOT_FROM, SLOT_TO do
		-- If all items are nil, either the player is naked or the API is just not ready.
		-- Just don't support naked players, who cares.
		if GetInventoryItemLink("player", i) ~= nil then ready = true end;
	end
	if ready and spec ~= nil then
		local guildName, guildRankName, guildRankIndex = GetGuildInfo("player");
		LazerArmoryGear["Guild"] = guildName;
		LazerArmoryGear["GuildRank"] = guildRankName;
		local name, _ = UnitName("player");
		local realm = GetRealmName();
		LazerArmoryGear["Name"] = name;
		LazerArmoryGear["Realm"] = realm;
		for i = SLOT_FROM, SLOT_TO do
			-- Item link also contains enchants, gems, etc:
			local link = GetInventoryItemLink("player", i);
			LazerArmoryGear["Spec" .. spec][i] = link;
			if link ~= nil then
				local itemId = link:match("item:(%d+):");
				local _, _, _, _, _, _, _, _, slot = GetItemInfo(itemId);
				LazerArmoryGear["Archive"][itemId] = slot;
			end
			-- That is all. Saving of the variable is done by WoW itself,
			-- because the variable is defined in LazerArmory.toc.
		end
	end
	LazerArmoryGear["Saved"] = Timestamp();
end

local function HookGearChanges()
	local frame = CreateFrame("FRAME", "LazerArmoryFrame");
	
	-- When logged in:
	frame:RegisterEvent("PLAYER_ENTERING_WORLD");
	-- When an item is equipped or unequipped:
	frame:RegisterEvent("UNIT_INVENTORY_CHANGED");
	-- Just to be sure. This will fire every couple of seconds:
	frame:RegisterEvent("SPELL_UPDATE_COOLDOWN");
	-- When logged out:
	frame:RegisterEvent("PLAYER_LOGOUT");
	
	-- Technically, only logout is necessary. However, we also want to save
	-- items in the player inventory and that might change throughout the
	-- game session (maybe he puts an item on and off and then we wouldn't
	-- know about it on logout. So we scan for changes more often.
	
	-- Also, let's react to when the saving environment is ready:
	frame:RegisterEvent("ADDON_LOADED");
	
	-- Since we are saving guild and rank now, let's also use these events.
	-- When the guild info is loaded (doesn't happen imidiately on login) or changed:
	frame:RegisterEvent("GUILD_ROSTER_UPDATE");
	-- Or when the player is promoted, kicked, etc.:
	frame:RegisterEvent("PLAYER_GUILD_UPDATE");
	
	frame:SetScript("OnEvent", SaveGear);
end

-- RUN:
HookGearChanges();