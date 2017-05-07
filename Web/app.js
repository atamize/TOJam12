/// <reference path="Photon/Photon-Javascript_SDK.d.ts"/> 
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
// For Photon Cloud Application access create cloud-app-info.js file in the root directory (next to default.html) and place next lines in it:
var AppInfo = {
    MasterAddress: "master server address:port",
    AppId: "52ae3714-5a54-46c9-a2e9-3ccfb7796de6",
    AppVersion: "0.1_1.83",
}
// fetching app info global variable while in global context
var DemoWss = this["AppInfo"] && this["AppInfo"]["Wss"];
var DemoAppId = this["AppInfo"] && this["AppInfo"]["AppId"] ? this["AppInfo"]["AppId"] : "<no-app-id>";
var DemoAppVersion = this["AppInfo"] && this["AppInfo"]["AppVersion"] ? this["AppInfo"]["AppVersion"] : "1.0";
var DemoMasterServer = this["AppInfo"] && this["AppInfo"]["MasterServer"];
var DemoFbAppId = this["AppInfo"] && this["AppInfo"]["FbAppId"];
var ConnectOnStart = false;

var LatestRoom;

var JoinRoom             = 0;
var StartGame            = 2;
var SubmitClue           = 3;
var SubmitGuess          = 4;
var BooPlayer            = 5;
var RestartGame          = 6;
var NewPlayers           = 7;

var SendTarget           = 8;
var InvalidClue          = 9;
var ValidClue            = 10;
var NextClue             = 11;
var Tally                = 12;
var UpdateWords          = 13;
var EnterGuess           = 14;

var disallowedWords;
var isGuessing = false;

function SetVisible(id, enable) {
    var elem = document.getElementById(id);
    if (enable)
        elem.style.display = '';
    else
        elem.style.display = 'none';
}

var DemoLoadBalancing = (function (_super) {
    __extends(DemoLoadBalancing, _super);
    function DemoLoadBalancing() {
        _super.call(this, DemoWss ? Photon.ConnectionProtocol.Wss : Photon.ConnectionProtocol.Ws, DemoAppId, DemoAppVersion);
        this.logger = new Exitgames.Common.Logger("Demo:");
        this.USERCOLORS = ["#FF0000", "#00AA00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF"];
        // uncomment to use Custom Authentication
        // this.setCustomAuthentication("username=" + "yes" + "&token=" + "yes");
        this.output(this.logger.format("Init", this.getNameServerAddress(), DemoAppId, DemoAppVersion));
        this.logger.info("Init", this.getNameServerAddress(), DemoAppId, DemoAppVersion);
        this.setLogLevel(Exitgames.Common.Logger.Level.DEBUG);
        this.myActor().setCustomProperty("color", this.USERCOLORS[0]);
    }
    DemoLoadBalancing.prototype.start = function () {
        this.setupUI();
        // connect if no fb auth required 
        if (ConnectOnStart) {
            if (DemoMasterServer) {
                this.setMasterServerAddress(DemoMasterServer);
                this.connect();
            }
            else {
                this.connectToRegionMaster("US");
            }
        }
    };
    DemoLoadBalancing.prototype.onError = function (errorCode, errorMsg) {
        this.output("Error " + errorCode + ": " + errorMsg);
    };
    DemoLoadBalancing.prototype.onEvent = function (code, content, actorNr) {
        this.logger.debug("onEvent", code, "content:", content, "actor:", actorNr);
        this.output("onEvent code: " + code);
        //this.output("onEvent actorNr: " + actorNr);
        this.output("onEvent content: " + content);

        switch (code) {
            case SendTarget:
                this.receiveTarget(content);
                break;

            case InvalidClue:
                this.output("Invalid clue");
                document.getElementById("clueField").value = "";
                SetVisible("errorMsg", true);
                break;

            case ValidClue:
                SetVisible("errorMsg", false);
                SetVisible("ClueScreen", false);
                SetVisible("WaitScreen", true);
                document.getElementById("wait").innerHTML = "Success! Please wait for everyone else to enter their clues.";
                break;

            case UpdateWords:
                document.getElementById("Disallowed").innerHTML = disallowedWords + ", " + content;
                break;

            case EnterGuess:
                if (isGuessing) {
                    SetVisible("WaitScreen", false);
                } else {
                    document.getElementById("wait").innerHTML = content + " is currently guessing.";
                }
                break;

            case NextClue:
                if (isGuessing) {
                    SetVisible("WaitScreen", false);
                    SetVisible("ClueScreen", true);
                    var split = content.split(";");
                    SetVisible("errorMsg", false);
                    SetVisible("disPart", false);
                    document.getElementById("Category").innerHTML = "Category: " + split[0];
                    document.getElementById("Target").innerHTML = "Clue: <b>" + split[1] + "</b>";
                    document.getElementById("fieldDesc").innerHTML = "Enter your guess:";
                }
                break;

            case Tally:
                SetVisible("ClueScreen", false);
                document.getElementById("wait").innerHTML = "Look at the scoreboard and wait for next round";
                SetVisible("WaitScreen", true);
                break;

            default:
                break;
        }
    };
    DemoLoadBalancing.prototype.onStateChange = function (state) {
        // "namespace" import for static members shorter acceess
        /*
        var LBC = Photon.LoadBalancing.LoadBalancingClient;
        var stateText = document.getElementById("statetxt");
        stateText.textContent = LBC.StateToName(state);
        this.updateRoomButtons();
        this.updateRoomInfo();
        */
    };
    DemoLoadBalancing.prototype.objToStr = function (x) {
        var res = "";
        for (var i in x) {
            res += (res == "" ? "" : " ,") + i + "=" + x[i];
        }
        return res;
    };
    DemoLoadBalancing.prototype.updateRoomInfo = function () {
        var btn = document.getElementById("startGame");

        if (this.myActor().actorNr == 2) {
            if (this.myRoomActorCount() > 3) {
                btn.disabled = false;
            } else {
                btn.disabled = true;
            }
        }

        /*
        var stateText = document.getElementById("roominfo");
        stateText.innerHTML = "room: " + this.myRoom().name + " [" + this.objToStr(this.myRoom()._customProperties) + "]";
        stateText.innerHTML = stateText.innerHTML + "<br>";
        stateText.innerHTML += " actors: ";
        stateText.innerHTML = stateText.innerHTML + "<br>";
        for (var nr in this.myRoomActors()) {
            var a = this.myRoomActors()[nr];
            stateText.innerHTML += " " + nr + " " + a.name + " [" + this.objToStr(a.customProperties) + "]";
            stateText.innerHTML = stateText.innerHTML + "<br>";
        }
        this.updateRoomButtons();
        */
    };
    DemoLoadBalancing.prototype.onActorPropertiesChange = function (actor) {
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.onMyRoomPropertiesChange = function () {
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.onRoomListUpdate = function (rooms, roomsUpdated, roomsAdded, roomsRemoved) {
        this.logger.info("Demo: onRoomListUpdate", rooms, roomsUpdated, roomsAdded, roomsRemoved);
        this.output("Demo: Rooms update: " + roomsUpdated.length + " updated, " + roomsAdded.length + " added, " + roomsRemoved.length + " removed");
        this.onRoomList(rooms);
        this.updateRoomButtons(); // join btn state can be changed
    };
    DemoLoadBalancing.prototype.onRoomList = function (rooms) {
        if (rooms.length > 0) {
            LatestRoom = rooms[0].name;

            var menu = document.getElementById("roomCode");
            menu.value = LatestRoom;
        }
        
        /*
        var menu = document.getElementById("gamelist");
        while (menu.firstChild) {
            menu.removeChild(menu.firstChild);
        }
        var selectedIndex = 0;
        for (var i = 0; i < rooms.length; ++i) {
            var r = rooms[i];
            var item = document.createElement("option");
            item.attributes["value"] = r.name;
            item.textContent = r.name;
            menu.appendChild(item);
            if (this.myRoom().name == r.name) {
                selectedIndex = i;
            }
        }
        menu.selectedIndex = selectedIndex;
        */
        this.output("Demo: Rooms total: " + rooms.length);
        this.updateRoomButtons();
    };
    DemoLoadBalancing.prototype.onJoinRoom = function () {
        this.output("Game " + this.myRoom().name + " joined");

        SetVisible("Login", false);
        SetVisible("WaitForPlayers", true);
        if (this.myActor().actorNr == 2) { 
            SetVisible("startGame", true);
        } else {
            SetVisible("startGame", false);
        }
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.onActorJoin = function (actor) {
        this.output("actor " + actor.actorNr + " joined");
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.onActorLeave = function (actor) {
        this.output("actor " + actor.actorNr + " left");
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.sendMessage = function (code, message) {
        try  {
			var options = { targetActors:[1] };
			this.raiseEvent(code, message, options);
            this.output('me[' + this.myActor().actorNr + ']: ' + message, this.myActor().getCustomProperty("color"));
        } catch (err) {
            this.output("error: " + err.message);
        }
    };
    DemoLoadBalancing.prototype.setupUI = function () {
        var _this = this;
        this.logger.info("Setting up UI.");
        var input = document.getElementById("name");
        input.value = 'Rando' + Math.floor((Math.random() * 10000) + 1);
        input.focus();

        var form = document.getElementById("mainfrm");
        form.onsubmit = function () {
            if (_this.isInLobby()) {
                if (input.value.length < 1)
                {
                    _this.output("Enter a name, doofus");
                }
                else
                {
                    _this.myActor().setName(input.value);
                    var roomCode = document.getElementById("roomCode");
                    if (roomCode.value.length > 0) {
                        _this.joinRoom(roomCode.value);
                    } else {
                        roomCode.focus();
                        _this.output("Enter a valid room code");
                    }
                }
            }
            else {
                _this.output("Reload page to connect to Master");
            }
            return false;
        };

        var startGameBtn = document.getElementById("startGame");
        startGameBtn.onclick = function () {
            _this.sendMessage(StartGame);
        };

        var submitClueBtn = document.getElementById("submitClueButton");
        submitClueBtn.onclick = function () {
            var clueField = document.getElementById("clueField");
            if (clueField.value.length > 0) {
                if (isGuessing) {
                    _this.sendMessage(SubmitGuess, clueField.value);
                    SetVisible("ClueScreen", false);
                    document.getElementById("wait").innerHTML = "Your guess is being evaluated...";
                    SetVisible("WaitScreen", true);
                } else {
                    _this.sendMessage(SubmitClue, clueField.value);
                }
            }
        };

        /*
        var submitGuessBtn = document.getElementById("submitGuessButton");
        submitGuessBtn.onclick = function () {
            if (submitGuessBtn.value.length > 0) {
                _this.sendMessage(SubmitGuess, submitGuessBtn.value);
            }
        };
        
        var btnJoin = document.getElementById("joingamebtn");
        btnJoin.onclick = function (ev) {
            if (_this.isInLobby()) {
                var menu = document.getElementById("gamelist");
                var gameId = menu.children[menu.selectedIndex].textContent;
                _this.output(gameId);
                _this.joinRoom(gameId);
            }
            else {
                _this.output("Reload page to connect to Master");
            }
            return false;
        };
        var btnJoin = document.getElementById("joinrandomgamebtn");
        btnJoin.onclick = function (ev) {
            if (_this.isInLobby()) {
                _this.output("Random Game...");
                _this.joinRandomRoom();
            }
            else {
                _this.output("Reload page to connect to Master");
            }
            return false;
        };
        var btnNew = document.getElementById("newgamebtn");
        btnNew.onclick = function (ev) {
            if (_this.isInLobby()) {
                var name = document.getElementById("newgamename");
                _this.output("New Game");
                _this.createRoom(name.value.length > 0 ? name.value : undefined);
            }
            else {
                _this.output("Reload page to connect to Master");
            }
            return false;
        };
        var form = document.getElementById("mainfrm");
        form.onsubmit = function () {
            if (_this.isJoinedToRoom()) {
                var input = document.getElementById("input");
                _this.sendMessage(input.value);
                input.value = '';
                input.focus();
            }
            else {
                if (_this.isInLobby()) {
                    _this.output("Press Join or New Game to connect to Game");
                }
                else {
                    _this.output("Reload page to connect to Master");
                }
            }
            return false;
        };
        var btn = document.getElementById("leavebtn");
        btn.onclick = function (ev) {
            _this.leaveRoom();
            return false;
        };
        btn = document.getElementById("colorbtn");
        btn.onclick = function (ev) {
            var ind = Math.floor(Math.random() * _this.USERCOLORS.length);
            var color = _this.USERCOLORS[ind];
            _this.myActor().setCustomProperty("color", color);
            _this.sendMessage("... changed his / her color!");
        };
        this.updateRoomButtons();
        */
    };
    DemoLoadBalancing.prototype.output = function (str, color) {
        var log = document.getElementById("status");
        log.innerHTML = str;
        console.log("BARF: " + str);
        /*
        var log = document.getElementById("theDialogue");
        var escaped = str.replace(/&/, "&amp;").replace(/</, "&lt;").
            replace(/>/, "&gt;").replace(/"/, "&quot;");
        if (color) {
            escaped = "<FONT COLOR='" + color + "'>" + escaped + "</FONT>";
        }
        log.innerHTML = log.innerHTML + escaped + "<br>";
        log.scrollTop = log.scrollHeight;
        */
    };
    DemoLoadBalancing.prototype.updateRoomButtons = function () {
        /*
        var btn;
        btn = document.getElementById("newgamebtn");
        btn.disabled = !(this.isInLobby() && !this.isJoinedToRoom());
        var canJoin = this.isInLobby() && !this.isJoinedToRoom() && this.availableRooms().length > 0;
        btn = document.getElementById("joingamebtn");
        btn.disabled = !canJoin;
        btn = document.getElementById("joinrandomgamebtn");
        btn.disabled = !canJoin;
        btn = document.getElementById("leavebtn");
        btn.disabled = !(this.isJoinedToRoom());
        */
    };

    DemoLoadBalancing.prototype.receiveTarget = function (message) {
        var split = message.split(";");
		var target = split[0];
		var category = split[1];
        disallowedWords = split[2];
        var guesser = split[3];
        var guesserId = parseInt(split[4]);

        //this.output("guesser Id: " + guesserId);
        //this.output("My actor num: " + this.myActor().actorNr);
        SetVisible("WaitForPlayers", false);
        if (guesserId == this.myActor().actorNr) {
            SetVisible("WaitScreen", true);
            document.getElementById("wait").innerHTML = "You're guessing this round. Sit back and wait until the other players have entered their clues!";
            isGuessing = true;
        } else {
            isGuessing = false;
            SetVisible("WaitScreen", false);
            document.getElementById("Category").innerHTML = "Category: " + category;
            document.getElementById("Target").innerHTML = "Target: <b>" + target + "</b>";
            document.getElementById("Disallowed").innerHTML = disallowedWords;
            document.getElementById("clueField").value = "";

            SetVisible("errorMsg", false);
            SetVisible("disPart", true);        
            document.getElementById("fieldDesc").innerHTML = "Enter your clue:";

            SetVisible("ClueScreen", true);
        }
    };
    return DemoLoadBalancing;
}(Photon.LoadBalancing.LoadBalancingClient));
var demo;
window.onload = function () {
    demo = new DemoLoadBalancing();
    demo.start();
};
