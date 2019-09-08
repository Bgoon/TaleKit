﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleKit.UI;
using GKit;

namespace TaleKit.Datas.Story.Jobs {
	/// <summary>
	/// StoryBlock에 붙는 컴포넌트이다. 편집기를 통해서 데이터를 수정할 수 있다.
	/// 이것을 상속받는 클래스는 생성자에서 부모만을 정해줘야 한다.
	/// </summary>
	public class BlockJobBase {
		protected static TaleKitClient Client => TaleKitClient.Instance;
		protected static UiManager UiManager => Client.UiManager;
		protected static GLoopEngine LoopEngine => Client.LoopEngine;
		

		public JobBlock OwnerBlock {
			get; private set;
		}

		public bool IsComplete {
			get; protected set;
		}

		public BlockJobBase(JobBlock ownerBlock) {
			this.OwnerBlock = ownerBlock;
		}

		public virtual void OnStart() {

		}
		public virtual void OnTick() {

		}
		public virtual void OnComplete() {

		}

		public virtual void Skip() {

		}
	}
}