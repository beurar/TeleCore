using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using RimWorld.Planet;
using Verse;

namespace TeleCore;

public class Scribe_Arrays
{
	public static void Look<T>(ref T[] arr, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
	{ 
		Scribe_Arrays.Look<T>(ref arr, false, label, lookMode, ctorArgs);
	}

	public static void Look<T>(ref T[] arr, bool saveDestroyedThings, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
	{
		if (lookMode == LookMode.Undefined && !Scribe_Universal.TryResolveLookMode(typeof(T), out lookMode, false, false))
		{
			TLog.Error("LookArray call with an array of " + typeof(T) + " must have lookMode set explicitly.");
			return;
		}

		if (Scribe.EnterNode(label))
		{
			try
			{
				if (Scribe.mode == LoadSaveMode.Saving)
				{
					if (arr == null)
					{
						Scribe.saver.WriteAttribute("IsNull", "True");
						return;
					}

					for (var i = 0; i < arr.Length; i++)
					{
						T t = arr[i];
						if (lookMode == LookMode.Value)
						{
							T t2 = t;
							Scribe_Values.Look<T>(ref t2, "li", default(T), true);
						}
						else if (lookMode == LookMode.LocalTargetInfo)
						{
							LocalTargetInfo localTargetInfo = (LocalTargetInfo) ((object) t);
							Scribe_TargetInfo.Look(ref localTargetInfo, saveDestroyedThings, "li");
						}
						else if (lookMode == LookMode.TargetInfo)
						{
							TargetInfo targetInfo = (TargetInfo) ((object) t);
							Scribe_TargetInfo.Look(ref targetInfo, saveDestroyedThings, "li");
						}
						else if (lookMode == LookMode.GlobalTargetInfo)
						{
							GlobalTargetInfo globalTargetInfo = (GlobalTargetInfo) ((object) t);
							Scribe_TargetInfo.Look(ref globalTargetInfo, saveDestroyedThings, "li");
						}
						else if (lookMode == LookMode.Def)
						{
							Def def = (Def) ((object) t);
							Scribe_Defs.Look<Def>(ref def, "li");
						}
						else if (lookMode == LookMode.BodyPart)
						{
							BodyPartRecord bodyPartRecord = (BodyPartRecord) ((object) t);
							Scribe_BodyParts.Look(ref bodyPartRecord, "li", null);
						}
						else if (lookMode == LookMode.Deep)
						{
							T t3 = t;
							Scribe_Deep.Look<T>(ref t3, saveDestroyedThings, "li", ctorArgs);
						}
						else if (lookMode == LookMode.Reference)
						{
							ILoadReferenceable loadReferenceable = (ILoadReferenceable) ((object) t);
							Scribe_References.Look<ILoadReferenceable>(ref loadReferenceable, "li",
								saveDestroyedThings);
						}
					}
				}

				if (Scribe.mode == LoadSaveMode.LoadingVars)
				{
					XmlNode curXmlParent = Scribe.loader.curXmlParent;
					XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
					if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
					{
						if (lookMode == LookMode.Reference)
						{
							Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, null);
						}

						arr = null;
					}
					else
					{
						if (lookMode == LookMode.Value)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = ScribeExtractor.ValueFromNode<T>((XmlNode) obj, default(T));
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.Deep)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = ScribeExtractor.SaveableFromNode<T>((XmlNode) obj, ctorArgs);
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.Def)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = ScribeExtractor.DefFromNodeUnsafe<T>((XmlNode) obj);
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.BodyPart)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = (T) ((object) ScribeExtractor.BodyPartFromNode((XmlNode) obj, i.ToString(), null));
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.LocalTargetInfo)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = (T) ((object) ScribeExtractor.LocalTargetInfoFromNode((XmlNode) obj, i.ToString(), LocalTargetInfo.Invalid));
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.TargetInfo)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = (T) ((object) ScribeExtractor.TargetInfoFromNode((XmlNode) obj, i.ToString(), TargetInfo.Invalid));
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.GlobalTargetInfo)
						{
							arr = new T[curXmlParent.ChildNodes.Count];
							for (var i = 0; i < curXmlParent.ChildNodes.Count; i++)
							{
								var obj = curXmlParent.ChildNodes[i];
								T item = (T) ((object) ScribeExtractor.GlobalTargetInfoFromNode((XmlNode) obj, i.ToString(), GlobalTargetInfo.Invalid));
								arr[i] = item;
							}
							return;
						}

						if (lookMode == LookMode.Reference)
						{
							List<string> list2 = new List<string>(curXmlParent.ChildNodes.Count);
							foreach (object obj8 in curXmlParent.ChildNodes)
							{
								XmlNode xmlNode = (XmlNode) obj8;
								list2.Add(xmlNode.InnerText);
							}

							Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(list2, "");
						}
					}
				}
				else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
				{
					if (lookMode == LookMode.Reference)
					{
						arr = Scribe.loader.crossRefs.TakeResolvedRefList<T>("").ToArray();
					}
					else if (lookMode == LookMode.LocalTargetInfo)
					{
						if (arr != null)
						{
							for (int i = 0; i < arr.Length; i++)
							{
								arr[i] = (T) ((object) ScribeExtractor.ResolveLocalTargetInfo((LocalTargetInfo) ((object) arr[i]), i.ToString()));
							}
						}
					}
					else if (lookMode == LookMode.TargetInfo)
					{
						if (arr != null)
						{
							for (int j = 0; j < arr.Length; j++)
							{
								arr[j] = (T) ((object) ScribeExtractor.ResolveTargetInfo((TargetInfo) ((object) arr[j]), j.ToString()));
							}
						}
					}
					else if (lookMode == LookMode.GlobalTargetInfo && arr != null)
					{
						for (int k = 0; k < arr.Length; k++)
						{
							arr[k] = (T) ((object) ScribeExtractor.ResolveGlobalTargetInfo((GlobalTargetInfo) ((object) arr[k]), k.ToString()));
						}
					}
				}

				return;
			}
			finally
			{
				Scribe.ExitNode();
			}
		}

		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			if (lookMode == LookMode.Reference)
			{
				Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, label);
			}
			arr = null;
		}
	}
}