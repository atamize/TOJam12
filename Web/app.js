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
    AppVersion: "0.2_1.83",
}
// fetching app info global variable while in global context
var DemoWss = this["AppInfo"] && this["AppInfo"]["Wss"];
var DemoAppId = this["AppInfo"] && this["AppInfo"]["AppId"] ? this["AppInfo"]["AppId"] : "<no-app-id>";
var DemoAppVersion = this["AppInfo"] && this["AppInfo"]["AppVersion"] ? this["AppInfo"]["AppVersion"] : "1.0";
var DemoMasterServer = this["AppInfo"] && this["AppInfo"]["MasterServer"];
var DemoFbAppId = this["AppInfo"] && this["AppInfo"]["FbAppId"];
var ConnectOnStart = false;

var DEBUG_MODE = false;
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
var GameOver             = 15;

var disallowedWords = "";
var isGuessing = false;
var previousPlayerName;
var previousRoomCode = null;

var AllElements = ["Login", "WaitForPlayers", "WaitScreen", "booOptions", "ClueScreen", "EndScreen"];

var LoginState = {
    elementsToOpen: ["Login"],
    elementsToClose: ["WaitForPlayers", "WaitScreen", "booOptions", "ClueScreen", "EndScreen"]
};

var WaitForPlayersState = {
    elementsToOpen: ["WaitForPlayers"],
    elementsToClose: ["Login", "WaitScreen", "booOptions", "ClueScreen", "EndScreen"]
};

var ClueState = {
    elementsToOpen: ["ClueScreen", "disPart"],
    elementsToClose: ["Login", "WaitScreen", "booOptions", "WaitForPlayers", "errorMsg", "EndScreen"]
};

var WaitForCluesState = {
    elementsToOpen: ["WaitScreen"],
    elementsToClose: ["Login", "booOptions", "WaitForPlayers", "ClueScreen", "EndScreen"]
};

var GuessState = {
    elementsToOpen: ["ClueScreen"],
    elementsToClose: ["Login", "WaitScreen", "booOptions", "WaitForPlayers", "errorMsg", "disPart", "EndScreen"]
};

var TallyState = {
    elementsToOpen: ["WaitScreen"],
    elementsToClose: ["Login", "booOptions", "WaitForPlayers", "ClueScreen", "EndScreen"]
};

var EndState = {
    elementsToOpen: ["EndScreen"],
    elementsToClose: ["Login", "booOptions", "WaitForPlayers", "ClueScreen", "WaitScreen"]
};

EnterState = function(state) {
    var i;
    for (i = 0; i < state.elementsToOpen.length; ++i) {
        SetVisible(state.elementsToOpen[i], true);
    }
    for (i = 0; i < state.elementsToClose.length; ++i) {
        SetVisible(state.elementsToClose[i], false);
    }
};

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
        //this.logger.debug("onEvent", code, "content:", content, "actor:", actorNr);
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
                document.getElementById("errorMsg").innerHTML = "Your clue cannot contain these words: " + content;
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
                    this.setWaitText(content + " is currently guessing.");
                    EnterState(WaitForCluesState);
                }
                break;

            case NextClue:
                isGuessing = true;
                EnterState(GuessState);
                var split = content.split(";");
                document.getElementById("Category").innerHTML = "Category: " + split[0];
                document.getElementById("Target").innerHTML = "Clue: <b>" + split[1] + "</b>";
                document.getElementById("fieldDesc").innerHTML = "Enter your guess:";
                document.getElementById("clueField").value = "";
                break;

            case Tally:
                EnterState(TallyState);
                disallowedWords = "";

                if (isGuessing) {
                   this.setWaitText("Look at the scoreboard and wait for next round");
                } else {
                    this.setWaitText("Think a certain clue sucks? Select it and hit the BOO button!");

                    if (content && content.length > 0) {
                        var split = content.split(";");
                        for (var i = 0; i < split.length; ++i) {
                            var btn = document.getElementById("radio" + i);
                            btn.style.display = '';
                            btn.value = i.toString();

                            var lbl = document.getElementById("rad" + i);
                            lbl.innerHTML = split[i];
                            lbl.style.display = '';
                        }

                        for ( ; i < 5; ++i) {
                            SetVisible("radio" + i, false);
                            SetVisible("rad" + i, false);
                        }

                        SetVisible("booOptions", true);
                    }
                }
                break;

            case GameOver:
                EnterState(EndState);
                break;

            default:
                break;
        }
    };

    DemoLoadBalancing.prototype.setWaitText = function (msg) {
        document.getElementById("wait").innerHTML = msg;
    };
    
    DemoLoadBalancing.prototype.onStateChange = function (state) {
        // "namespace" import for static members shorter acceess
        var LBC = Photon.LoadBalancing.LoadBalancingClient;
        if (LBC.StateToName(state) == 'JoinedLobby') {
            if (previousRoomCode != null && previousRoomCode.length > 0) {
                this.myActor().setName(previousPlayerName);
                this.joinRoom(previousRoomCode);
            }
            this.output("Joined lobby");
        }
        //var stateText = document.getElementById("statetxt");
        //stateText.textContent = LBC.StateToName(state);
        //this.updateRoomButtons();
        //this.updateRoomInfo();
        
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

            if (DEBUG_MODE) {
                var menu = document.getElementById("roomCode");
                menu.value = LatestRoom;
            }
        }
        
        this.output("Demo: Rooms total: " + rooms.length);
        this.updateRoomButtons();
    };
    DemoLoadBalancing.prototype.onJoinRoom = function () {
        this.output("Game " + this.myRoom().name + " joined");

        if (previousRoomCode == null) {
            previousRoomCode = this.myRoom().name;
            previousPlayerName = this.myActor().name;

            EnterState(WaitForPlayersState);
            if (this.myActor().actorNr == 2) { 
                SetVisible("startGame", true);
            } else {
                SetVisible("startGame", false);
            }
            this.updateRoomInfo();
        }
    };
    DemoLoadBalancing.prototype.onActorJoin = function (actor) {
        this.output("actor " + actor.actorNr + " joined");
        this.updateRoomInfo();
    };
    DemoLoadBalancing.prototype.onActorLeave = function (actor) {
        this.output("actor " + actor.actorNr + " left");
        this.updateRoomInfo();

        if (this.isJoinedToRoom() && actor.actorNr == 1) {
            this.disconnect();
            SetVisible("ClueScreen", false);
            SetVisible("WaitForPlayers", false);
            SetVisible("WaitForPlayers", false);
            SetVisible("WaitScreen", true);
            SetVisible("booOptions", false);
            SetVisible("EndScreen", false);
            document.getElementById("wait").innerHTML = "You have been disconnected. Please refresh the page to restart.";
        }
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
        
        input.onclick = function() {
            this.select();
        };

        var roomCode = document.getElementById("roomCode");
        roomCode.focus();

        var form = document.getElementById("mainfrm");
        form.onsubmit = function () {
            if (_this.isInLobby()) {
                if (input.value.length < 1)
                {
                    _this.output("Enter a name, doofus");
                }
                else
                {
                    _this.myActor().setName(input.value.toUpperCase());
                    
                    if (roomCode.value.length > 0) {
                        _this.joinRoom(roomCode.value.toUpperCase());
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
                    _this.sendMessage(SubmitGuess, clueField.value.toUpperCase());
                    SetVisible("ClueScreen", false);
                    document.getElementById("wait").innerHTML = "Your guess is being evaluated...";
                    SetVisible("WaitScreen", true);
                } else {
                    _this.sendMessage(SubmitClue, clueField.value.toUpperCase());
                }
            }
        };

        var booBtn = document.getElementById("booButton");
        booBtn.onclick = function() {
            for (var i = 0; i < 5; ++i) {
                var btn = document.getElementById("radio" + i);
                if (btn.checked) {
                    _this.sendMessage(BooPlayer, i);
                    break;
                }
            }
            SetVisible("booOptions", false);
        };

        var newPlayers = document.getElementById("newGame");
        newPlayers.onclick = function() {
            _this.sendMessage(NewPlayers);
        };

        var restartGame = document.getElementById("samePlayers");
        restartGame.onclick = function() {
            _this.sendMessage(RestartGame);
        };
    };
    DemoLoadBalancing.prototype.output = function (str, color) {
        var log = document.getElementById("status");
        log.innerHTML = str;
        log.scrollTop = log.scrollHeight;
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

    };

    DemoLoadBalancing.prototype.receiveTarget = function (message) {
        var split = message.split(";");
		var target = split[0];
		var category = split[1];

        if (disallowedWords == null || disallowedWords == "") {
            disallowedWords = split[2];
        }

        var guesser = split[3];
        var guesserId = parseInt(split[4]);

        //this.output("guesser Id: " + guesserId);
        //this.output("My actor num: " + this.myActor().actorNr);
        if (guesserId == this.myActor().actorNr) {
            EnterState(WaitForCluesState);
            this.setWaitText("You're guessing this round. Sit back and wait until the other players have entered their clues!");
            isGuessing = true;
        } else {
            isGuessing = false;
            EnterState(ClueState);
            document.getElementById("Category").innerHTML = "Category: " + category;
            document.getElementById("Target").innerHTML = "Target: <b>" + target + "</b>";
            document.getElementById("Disallowed").innerHTML = disallowedWords;
            document.getElementById("clueField").value = "";      
            document.getElementById("fieldDesc").innerHTML = "Enter your clue:";
        }
    };
    return DemoLoadBalancing;
}(Photon.LoadBalancing.LoadBalancingClient));

var demo;

window.onload = function () {
    demo = new DemoLoadBalancing();
    demo.start();
};

var hidden, visibilityChange; 
if (typeof document.hidden !== "undefined") { // Opera 12.10 and Firefox 18 and later support 
  hidden = "hidden";
  visibilityChange = "visibilitychange";
} else if (typeof document.msHidden !== "undefined") {
  hidden = "msHidden";
  visibilityChange = "msvisibilitychange";
} else if (typeof document.webkitHidden !== "undefined") {
  hidden = "webkitHidden";
  visibilityChange = "webkitvisibilitychange";
}

function handleVisibilityChange() {
  if (document[hidden]) {
  } else {
    if (!demo.isInLobby()) {
        demo.connect();
    }
  }
}

// Warn if the browser doesn't support addEventListener or the Page Visibility API
if (typeof document.addEventListener === "undefined" || typeof document[hidden] === "undefined") {
  alert("This game requires a browser, such as Google Chrome or Firefox, that supports the Page Visibility API.");
} else {
  // Handle page visibility change   
  document.addEventListener(visibilityChange, handleVisibilityChange, false);
}
