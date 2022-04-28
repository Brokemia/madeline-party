using Celeste;
using Celeste.Editor;
using Celeste.Mod;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MadelineParty.BoardController;

namespace MadelineParty.Tools {
	public class BoardEditor : Scene {
		private enum MouseModes {
			Hover,
			Pan,
			Select,
			Move,
			Resize
		}

		private static readonly Color gridColor = new Color(0.1f, 0.1f, 0.1f);

		public static readonly int coordScale = 2;

		private static Camera Camera;

		private float fade = 0f;

		private static float saveFlash = 0f;

		private List<BoardSpaceTemplate> spaces;

		private Dictionary<BoardSpaceTemplate, HashSet<BoardSpaceTemplate>> connections = new Dictionary<BoardSpaceTemplate, HashSet<BoardSpaceTemplate>>();

		private Vector2 mousePosition;

		private MouseModes mouseMode;

		private Vector2 lastMouseScreenPosition;

		private Vector2 mouseDragStart;

		private HashSet<BoardSpaceTemplate> selection;

		private HashSet<BoardSpaceTemplate> hovered;

		private List<Vector2[]> undoStack;

		private List<Vector2[]> redoStack;

		private const string ManualText = "Right Click:  Teleport to the room\nConfirm:      Teleport to the room\nHold Control: Restart Chapter before teleporting\nHold Shift:   Teleport to the mouse position\nCancel:       Exit debug map\nQ:            Show red berries\nF1:           Show keys\nF2:           Center on current respawn point\nF5:           Show/Hide instructions";

		private const string MinimalManualText = "F5: Show/Hide instructions";

		private static readonly int ZoomIntervalFrames = 6;

		private int zoomWaitFrames;

		public BoardEditor() {
			spaces = new List<BoardSpaceTemplate>();
			selection = new HashSet<BoardSpaceTemplate>();
			hovered = new HashSet<BoardSpaceTemplate>();
			undoStack = new List<Vector2[]>();
			redoStack = new List<Vector2[]>();
			foreach (BoardSpace space in boardSpaces) {
				spaces.Add(new BoardSpaceTemplate(space));
			}
			foreach(BoardSpaceTemplate temp in spaces) {
				connections[temp] = new HashSet<BoardSpaceTemplate>();
				foreach(BoardSpace con in temp.originalSpace.destinations) {
					Console.WriteLine("From " + temp.originalSpace.ID + " to " + con.ID);
					connections[temp].Add(spaces.Find((s) => s.originalSpace.ID.Equals(con.ID)));
                }
            }
			Camera = new Camera();
			Camera.Zoom = 6f;
			Camera.CenterOrigin();
			if (SaveData.Instance == null) {
				SaveData.InitializeDebugMode();
			}
		}

		public override void GainFocus() {
			base.GainFocus();
			SaveAndReload();
		}

		private void SelectAll() {
			selection.Clear();
			foreach (BoardSpaceTemplate level in spaces) {
				selection.Add(level);
			}
		}

		private void Save() {
			int nextID = 0;
			boardSpaces.Clear();
			foreach(BoardSpaceTemplate template in spaces) {
				template.originalSpace = new BoardSpace() {
					ID = nextID,
					x = template.X / coordScale,
					y = template.Y / coordScale,
					type = template.Type,
					heartSpace = template.HeartSpace,
					destinations = new List<BoardSpace>(),
					greenSpaceEvent = template.GreenSpaceEvent
				};
				nextID++;
            }
			foreach(BoardSpaceTemplate template in spaces) {
				if(!connections.ContainsKey(template) || connections[template] == null) {
					connections[template] = new HashSet<BoardSpaceTemplate>();
                }
				foreach (BoardSpaceTemplate con in connections[template]) {
					template.originalSpace.destinations.Add(con.originalSpace);
                }
				boardSpaces.Add(template.originalSpace);
            }
			Console.WriteLine("Saving!");
			foreach(BoardSpaceTemplate template in spaces) {
				Console.WriteLine(template.originalSpace.ToString());
			}
		}

		private void SaveAndReload() {
			Save();
		}

		private void UpdateMouse() {
			mousePosition = Vector2.Transform(MInput.Mouse.Position, Matrix.Invert(Camera.Matrix));
		}

		public override void Update() {
			MakeMapEditorBetter();
			Vector2 vector = default(Vector2);
			vector.X = (lastMouseScreenPosition.X - MInput.Mouse.Position.X) / Camera.Zoom;
			vector.Y = (lastMouseScreenPosition.Y - MInput.Mouse.Position.Y) / Camera.Zoom;
			if (MInput.Keyboard.Pressed(Keys.Space) && MInput.Keyboard.Check(Keys.LeftControl)) {
				Camera.Zoom = 6f;
				Camera.Position = Vector2.Zero;
			}
			int zoomDir = Math.Sign(MInput.Mouse.WheelDelta);
			if ((zoomDir > 0 && Camera.Zoom >= 1f) || Camera.Zoom > 1f) {
				Camera.Zoom += zoomDir;
			} else {
				Camera.Zoom += zoomDir * 0.25f;
			}
			Camera.Zoom = Math.Max(0.25f, Math.Min(24f, Camera.Zoom));
			Camera.Position += new Vector2(Input.MoveX.Value, Input.MoveY.Value) * 300f * Engine.DeltaTime;
			UpdateMouse();
			hovered.Clear();
			if (mouseMode == MouseModes.Hover) {
				mouseDragStart = mousePosition;
				if (MInput.Mouse.PressedLeftButton) {
					bool flag = LevelCheck(mousePosition);
					if (MInput.Keyboard.Check(Keys.Space)) {
						mouseMode = MouseModes.Pan;
					} else if (MInput.Keyboard.Check(Keys.P)) {
						BoardSpaceTemplate pathDest = TestCheck(mousePosition);
						BoardSpaceTemplate pathSrc;
						if(pathDest != null && selection.Count == 1 && (pathSrc = selection.First()) != pathDest) {
							if (!connections.ContainsKey(pathSrc)) {
								connections[pathSrc] = new HashSet<BoardSpaceTemplate>();
							}
							if (connections[pathSrc].Contains(pathDest)) {
								connections[pathSrc].Remove(pathDest);
							} else {
								connections[pathSrc].Add(pathDest);
							}
							selection.Clear();
							selection.Add(pathDest);
                        }
					} else if (MInput.Keyboard.Check(Keys.LeftControl)) {
						if (flag) {
							ToggleSelection(mousePosition);
						} else {
							mouseMode = MouseModes.Select;
						}
					} else if (MInput.Keyboard.Check(Keys.N)) {
						spaces.Add(new BoardSpaceTemplate(new BoardSpace { x = (int)mousePosition.X / 2, y = (int)mousePosition.Y / 2, type = 'b' }));
					} else if (flag) {
						if (!SelectionCheck(mousePosition)) {
							SetSelection(mousePosition);
						}

						StoreUndo();
						foreach (BoardSpaceTemplate item3 in selection) {
							item3.StartMoving();
						}
						mouseMode = MouseModes.Move;
					} else {
						mouseMode = MouseModes.Select;
					}
				} else if (MInput.Mouse.PressedRightButton) {
					BoardSpaceTemplate space = TestCheck(mousePosition);
					if (space != null) {
						if (MInput.Keyboard.Check(Keys.N)) {
							spaces.Remove(space);
						}
						return;
					}
				} else if (MInput.Mouse.PressedMiddleButton) {
					mouseMode = MouseModes.Pan;
				} else if (!MInput.Keyboard.Check(Keys.Space)) {
					foreach (BoardSpaceTemplate space in spaces) {
						if (space.Check(mousePosition)) {
							hovered.Add(space);
						}
					}
					if (MInput.Keyboard.Check(Keys.LeftControl)) {
						if (MInput.Keyboard.Pressed(Keys.Z)) {
							Undo();
						} else if (MInput.Keyboard.Pressed(Keys.Y) || (MInput.Keyboard.Check(Keys.LeftShift) && MInput.Keyboard.Pressed(Keys.Z))) {
							Redo();
						} else if (MInput.Keyboard.Pressed(Keys.A)) {
							SelectAll();
						}
					}
				}
			} else if (mouseMode == MouseModes.Pan) {
				Camera.Position += vector;
				if (!MInput.Mouse.CheckLeftButton && !MInput.Mouse.CheckMiddleButton) {
					mouseMode = MouseModes.Hover;
				}
			} else if (mouseMode == MouseModes.Select) {
				Rectangle mouseRect = GetMouseRect(mouseDragStart, mousePosition);
				foreach (BoardSpaceTemplate space in spaces) {
					if (space.Check(mouseRect)) {
						hovered.Add(space);
					}
				}
				if (!MInput.Mouse.CheckLeftButton) {
					if (MInput.Keyboard.Check(Keys.LeftControl)) {
						ToggleSelection(mouseRect);
					} else {
						SetSelection(mouseRect);
					}
					mouseMode = MouseModes.Hover;
				}
			} else if (mouseMode == MouseModes.Move) {
				Vector2 relativeMove = coordScale * ((mousePosition - mouseDragStart) / coordScale).Round();
				bool snap = selection.Count == 1 && !MInput.Keyboard.Check(Keys.LeftAlt);
				foreach (BoardSpaceTemplate space in selection) {
					space.Move(relativeMove);
				}
				if (!MInput.Mouse.CheckLeftButton) {
					mouseMode = MouseModes.Hover;
				}
			}
			if (MInput.Keyboard.Pressed(Keys.D1)) {
				SetType('b');
			} else if (MInput.Keyboard.Pressed(Keys.D2)) {
				SetType('r');
			} else if (MInput.Keyboard.Pressed(Keys.D3)) {
				SetType('i');
			} else if (MInput.Keyboard.Pressed(Keys.D4)) {
				SetType('s');
			} else if (MInput.Keyboard.Pressed(Keys.D5)) {
				SetType('g');
				SetGreenSpaceEvent(DebugCommands.greenSpaceEvent);
            }
			if(MInput.Keyboard.Pressed(Keys.H)) {
				ToggleHeartSpace();
            }
			if (MInput.Keyboard.Pressed(Keys.F1) || (MInput.Keyboard.Check(Keys.LeftControl) && MInput.Keyboard.Pressed(Keys.S))) {
				SaveAndReload();
				return;
			}
			if (saveFlash > 0f) {
				saveFlash -= Engine.DeltaTime * 4f;
			}
			lastMouseScreenPosition = MInput.Mouse.Position;
			base.Update();
		}

		private void SetType(char type) {
			foreach (BoardSpaceTemplate item in selection) {
				item.Type = type;
			}
		}

		private void SetGreenSpaceEvent(string gse) {
			foreach (BoardSpaceTemplate item in selection) {
				item.GreenSpaceEvent = gse;
			}
		}

		private void ToggleHeartSpace() {
			foreach (BoardSpaceTemplate item in selection) {
				item.HeartSpace = !item.HeartSpace;
			}
		}

		public void DrawArrow(Vector2 src, Vector2 dest) {
			Draw.Line(src, dest, Color.White);
			Draw.Line(src + (dest - src) / 2, src + (dest - src) / 3 + 4 * (dest - src).Perpendicular().SafeNormalize(), Color.White);
			Draw.Line(src + (dest - src) / 2, src + (dest - src) / 3 - 4 * (dest - src).Perpendicular().SafeNormalize(), Color.White);
		}

		public override void Render() {
			UpdateMouse();
			Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Camera.Matrix * Engine.ScreenMatrix);
			float num = 1920f / Camera.Zoom;
			float num2 = 1080f / Camera.Zoom;
			int num3 = 5;
			float num4 = (float)Math.Floor(Camera.Left / num3 - 1f) * num3;
			float num5 = (float)Math.Floor(Camera.Top / num3 - 1f) * num3;
			for (float num6 = num4; num6 <= num4 + num + 10f; num6 += 5f) {
				Draw.Line(num6, Camera.Top, num6, Camera.Top + num2, gridColor);
			}
			for (float num7 = num5; num7 <= num5 + num2 + 10f; num7 += 5f) {
				Draw.Line(Camera.Left, num7, Camera.Left + num, num7, gridColor);
			}
			Draw.Line(0f, Camera.Top, 0f, Camera.Top + num2, Color.DarkSlateBlue, 1f / Camera.Zoom);
			Draw.Line(Camera.Left, 0f, Camera.Left + num, 0f, Color.DarkSlateBlue, 1f / Camera.Zoom);

			foreach(BoardSpaceTemplate src in connections.Keys) {
				foreach(BoardSpaceTemplate dest in connections[src]) {
					DrawArrow(src.Position, dest.Position);
				}
            }

			// Draw line for current path being built
			if (MInput.Keyboard.Check(Keys.P)) {
				BoardSpaceTemplate pathDest = TestCheck(mousePosition);
				BoardSpaceTemplate pathSrc;
				if (selection.Count == 1 && (pathSrc = selection.First()) != pathDest) {
					DrawArrow(pathSrc.Position, pathDest?.Position ?? mousePosition);
				}
			}

			foreach (BoardSpaceTemplate space in spaces) {
				space.Render(Camera);
			}
			if (mouseMode == MouseModes.Hover) {
				Draw.Line(mousePosition.X - 12f / Camera.Zoom, mousePosition.Y, mousePosition.X + 12f / Camera.Zoom, mousePosition.Y, Color.Yellow, 3f / Camera.Zoom);
				Draw.Line(mousePosition.X, mousePosition.Y - 12f / Camera.Zoom, mousePosition.X, mousePosition.Y + 12f / Camera.Zoom, Color.Yellow, 3f / Camera.Zoom);
			} else if (mouseMode == MouseModes.Select) {
				Draw.Rect(GetMouseRect(mouseDragStart, mousePosition), Color.Lime * 0.25f);
			}
			if (saveFlash > 0f) {
				Draw.Rect(Camera.Left, Camera.Top, num, num2, Color.White * Ease.CubeInOut(saveFlash));
			}
			if (fade > 0f) {
				Draw.Rect(0f, 0f, 320f, 180f, Color.Black * fade);
			}
			Draw.SpriteBatch.End();
			Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
			Draw.Rect(0f, 0f, 1920f, 72f, Color.Black);
			Vector2 position = new Vector2(16f, 4f);
			Vector2 position2 = new Vector2(1904f, 4f);
			if (hovered.Count == 0) {
				if (selection.Count > 0) {
					ActiveFont.Draw(selection.Count + " spaces selected", position, Color.Red);
				} else {
					ActiveFont.Draw("Hello", position, Color.Aqua);
				}
			} else if (hovered.Count == 1) {
				BoardSpaceTemplate levelTemplate = null;
                using (HashSet<BoardSpaceTemplate>.Enumerator enumerator2 = hovered.GetEnumerator()) {
                    if (enumerator2.MoveNext()) {
                        levelTemplate = enumerator2.Current;
                    }
                }
				string text = levelTemplate.X + "," + levelTemplate.Y + "   " + levelTemplate.Type + "   " + levelTemplate.HeartSpace;
				if (selection.Count > 0) {
					ActiveFont.Draw(selection.Count + " spaces selected", position, Color.Red);
				} else {
					ActiveFont.Draw("Hello", position, Color.Aqua);
				}
				ActiveFont.Draw(text, position2, Vector2.UnitX, Vector2.One, Color.Green);
			} else {
				ActiveFont.Draw(hovered.Count + " spaces", position, Color.Yellow);
			}
			Draw.SpriteBatch.End();
			RenderManualText();
		}

		private void StoreUndo() {
			Vector2[] array = new Vector2[spaces.Count];
			for (int i = 0; i < spaces.Count; i++) {
				array[i] = new Vector2(spaces[i].X, spaces[i].Y);
			}
			undoStack.Add(array);
			while (undoStack.Count > 30) {
				undoStack.RemoveAt(0);
			}
			redoStack.Clear();
		}

		private void Undo() {
			if (undoStack.Count > 0) {
				Vector2[] array = new Vector2[spaces.Count];
				for (int i = 0; i < spaces.Count; i++) {
					array[i] = new Vector2(spaces[i].X, spaces[i].Y);
				}
				redoStack.Add(array);
				Vector2[] array2 = undoStack[undoStack.Count - 1];
				undoStack.RemoveAt(undoStack.Count - 1);
				for (int j = 0; j < array2.Length; j++) {
					spaces[j].X = (int)array2[j].X;
					spaces[j].Y = (int)array2[j].Y;
				}
			}
		}

		private void Redo() {
			if (redoStack.Count > 0) {
				Vector2[] array = new Vector2[spaces.Count];
				for (int i = 0; i < spaces.Count; i++) {
					array[i] = new Vector2(spaces[i].X, spaces[i].Y);
				}
				undoStack.Add(array);
				Vector2[] array2 = redoStack[undoStack.Count - 1];
				redoStack.RemoveAt(undoStack.Count - 1);
				for (int j = 0; j < array2.Length; j++) {
					spaces[j].X = (int)array2[j].X;
					spaces[j].Y = (int)array2[j].Y;
				}
			}
		}

		private Rectangle GetMouseRect(Vector2 a, Vector2 b) {
			Vector2 vector = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
			Vector2 vector2 = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
			return new Rectangle((int)vector.X, (int)vector.Y, (int)(vector2.X - vector.X), (int)(vector2.Y - vector.Y));
		}

		private BoardSpaceTemplate TestCheck(Vector2 point) {
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(point)) {
					return space;
				}
			}
			return null;
		}

		private bool LevelCheck(Vector2 point) {
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(point)) {
					return true;
				}
			}
			return false;
		}

		private bool SelectionCheck(Vector2 point) {
			foreach (BoardSpaceTemplate space in selection) {
				if (space.Check(point)) {
					return true;
				}
			}
			return false;
		}

		private bool SetSelection(Vector2 point) {
			selection.Clear();
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(point)) {
					selection.Add(space);
				}
			}
			return selection.Count > 0;
		}

		private bool ToggleSelection(Vector2 point) {
			bool result = false;
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(point)) {
					result = true;
					if (selection.Contains(space)) {
						selection.Remove(space);
					} else {
						selection.Add(space);
					}
				}
			}
			return result;
		}

		private void SetSelection(Rectangle rect) {
			selection.Clear();
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(rect)) {
					selection.Add(space);
				}
			}
		}

		private void ToggleSelection(Rectangle rect) {
			foreach (BoardSpaceTemplate space in spaces) {
				if (space.Check(rect)) {
					if (selection.Contains(space)) {
						selection.Remove(space);
					} else {
						selection.Add(space);
					}
				}
			}
		}

		private void MakeMapEditorBetter() {
			if (Camera != null) {
				Vector2 vector = new Vector2(Input.MoveX.Value, Input.MoveY.Value) * 300f * Engine.DeltaTime;
				Camera.Position -= vector;
				Vector2 vector2 = new Vector2(vector.X, vector.Y);
				if (Camera.Zoom < 6f) {
					Camera.Position += vector2 * (float)Math.Pow(1.3, 6f - Camera.Zoom);
				} else {
					Camera.Position += vector2;
				}
			}
			GamePadState currentState = MInput.GamePads[Input.Gamepad].CurrentState;
			if (zoomWaitFrames <= 0 && Camera != null) {
				float num = 0f;
				if (Math.Abs(currentState.ThumbSticks.Right.X) >= 0.5f) {
					num = Camera.Zoom + Math.Sign(currentState.ThumbSticks.Right.X) * 1f;
				} else if (Math.Abs(currentState.ThumbSticks.Right.Y) >= 0.5f) {
					num = Camera.Zoom + Math.Sign(currentState.ThumbSticks.Right.Y) * 1f;
				}
				if (num >= 1f) {
					Camera.Zoom = num;
					zoomWaitFrames = ZoomIntervalFrames;
				}
			}
		}

        private void RenderManualText() {
			if (MInput.Keyboard.Pressed(Keys.F5)) {
				CoreModule.Settings.ShowManualTextOnDebugMap = !CoreModule.Settings.ShowManualTextOnDebugMap;
			}
			Draw.SpriteBatch.Begin();
			string text = MinimalManualText;
            if (CoreModule.Settings.ShowManualTextOnDebugMap) {
				text = ManualText;
			}
			Vector2 vector = Draw.DefaultFont.MeasureString(text);
			//Draw.Rect(Engine.ViewWidth - vector.X - 20f, Engine.ViewHeight - vector.Y - 20f, vector.X + 20f, vector.Y + 20f, Color.Black * 0.8f);
			//Draw.SpriteBatch.DrawString(Draw.DefaultFont, text, new Vector2(Engine.ViewWidth - vector.X - 10f, Engine.ViewHeight - vector.Y - 10f), Color.White);
			Draw.SpriteBatch.End();
		}
	}
}